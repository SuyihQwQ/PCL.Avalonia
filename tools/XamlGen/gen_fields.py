#!/usr/bin/env python3
# SPDX-License-Identifier: Apache-2.0
"""
为 Avalonia .axaml 中的 x:Name 生成 VB 字段与 InitControls()。

Avalonia 的 XAML 名称生成器只支持 C#，VB 项目需要自己补上：
    Friend WithEvents <Name> As <Type>
    InitControls()  ->  AvaloniaXamlLoader.Load(Me) + 逐个 FindControl(Of T)

用法：
    python3 tools/XamlGen/gen_fields.py                 # 生成到 PCL/Generated
    python3 tools/XamlGen/gen_fields.py --check         # 校验是否有漂移
    python3 tools/XamlGen/gen_fields.py --project path  # 指定项目目录
"""

from __future__ import annotations

import argparse
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

XAML_NS = "http://schemas.microsoft.com/winfx/2006/xaml"
AVALONIA_NS = "https://github.com/avaloniaui"

SKIP_SUBTREES = {
    "DataTemplate",
    "TreeDataTemplate",
    "ControlTemplate",
    "ItemsPanelTemplate",
    "Style",
    "ResourceDictionary",
}

SHAPE_TYPES = {
    "Path", "Rectangle", "Ellipse", "Line", "Polygon", "Polyline",
}

DOCUMENT_TYPES = {
    "Run", "Span", "Bold", "Italic", "Underline", "LineBreak", "Hyperlink",
}

PRIMITIVE_TYPES = {
    "Popup", "ScrollBar", "Track", "Thumb", "RepeatButton",
    "TemplatedControl", "AdornerLayer", "OverlayLayer", "VisualLayerManager",
}

VB_KEYWORDS = {
    "AddHandler", "AddressOf", "Alias", "And", "AndAlso", "As", "Boolean", "ByRef", "Byte",
    "ByVal", "Call", "Case", "Catch", "CBool", "CByte", "CChar", "CDate", "CDbl", "CDec",
    "Char", "CInt", "Class", "CLng", "CObj", "Const", "Continue", "CSByte", "CShort",
    "CSng", "CStr", "CType", "CUInt", "CULng", "CUShort", "Date", "Decimal", "Declare",
    "Default", "Delegate", "Dim", "DirectCast", "Do", "Double", "Each", "Else", "ElseIf",
    "End", "EndIf", "Enum", "Erase", "Error", "Event", "Exit", "False", "Finally", "For",
    "Friend", "Function", "Get", "GetType", "GetXMLNamespace", "Global", "GoSub", "GoTo",
    "Handles", "If", "Implements", "Imports", "In", "Inherits", "Integer", "Interface",
    "Is", "IsNot", "Let", "Lib", "Like", "Long", "Loop", "Me", "Mod", "Module", "MustInherit",
    "MustOverride", "MyBase", "MyClass", "Namespace", "Narrowing", "New", "Next", "Not",
    "Nothing", "NotInheritable", "NotOverridable", "Object", "Of", "On", "Operator", "Option",
    "Optional", "Or", "OrElse", "Out", "Overloads", "Overridable", "Overrides", "ParamArray",
    "Partial", "Private", "Property", "Protected", "Public", "RaiseEvent", "ReadOnly",
    "ReDim", "REM", "RemoveHandler", "Resume", "Return", "SByte", "Select", "Set", "Shadows",
    "Shared", "Short", "Single", "Static", "Step", "Stop", "String", "Structure", "Sub",
    "SyncLock", "Then", "Throw", "To", "True", "Try", "TryCast", "TypeOf", "UInteger",
    "ULong", "UShort", "Using", "Variant", "Wend", "When", "While", "Widening", "With",
    "WithEvents", "WriteOnly", "Xor",
}


def split_tag(tag: str) -> tuple[str, str]:
    if tag.startswith("{"):
        uri, local = tag[1:].split("}", 1)
        return uri, local
    return "", tag


def collect_namespaces(path: Path) -> dict[str, str]:
    """返回 uri -> 原始 xmlns 声明（用于 clr-namespace 解析）。"""
    result: dict[str, str] = {}
    for _, (prefix, uri) in ET.iterparse(path, events=("start-ns",)):
        result.setdefault(uri, prefix)
    return result


def clr_namespace(uri: str) -> str | None:
    match = re.match(r"(?:clr-namespace|using):([^;]+)", uri)
    if match:
        return match.group(1).strip()
    return None


def resolve_type(uri: str, local: str, uri_clr: dict[str, str]) -> str | None:
    if uri == AVALONIA_NS:
        if local in SHAPE_TYPES:
            return f"Avalonia.Controls.Shapes.{local}"
        if local in DOCUMENT_TYPES:
            return f"Avalonia.Controls.Documents.{local}"
        if local in PRIMITIVE_TYPES:
            return f"Avalonia.Controls.Primitives.{local}"
        return f"Avalonia.Controls.{local}"
    if uri in uri_clr:
        return f"{uri_clr[uri]}.{local}"
    return None


def vb_identifier(name: str) -> str:
    if name in VB_KEYWORDS:
        return f"[{name}]"
    return name


def pascal_case(name: str) -> str:
    return name[:1].upper() + name[1:]


def scan_names(path: Path) -> tuple[str, list[tuple[str, str]], list[str]]:
    """返回 (x:Class, [(名称, 类型), ...], 警告列表)。"""
    text = path.read_text(encoding="utf-8-sig")
    if "<!DOCTYPE" in text:
        raise ValueError(f"{path}: 拒绝解析带 DOCTYPE 的 XAML")

    namespaces = collect_namespaces(path)
    uri_clr = {}
    for uri, _prefix in namespaces.items():
        clr = clr_namespace(uri)
        if clr:
            uri_clr[uri] = clr

    tree = ET.parse(path)
    root = tree.getroot()
    x_class = root.get(f"{{{XAML_NS}}}Class")
    if not x_class:
        return "", [], []

    found: list[tuple[str, str]] = []
    seen: set[str] = set()
    warnings: list[str] = []

    def walk(element: ET.Element, skip_depth: int) -> None:
        uri, local = split_tag(element.tag)
        if local in SKIP_SUBTREES:
            skip_depth += 1

        if skip_depth <= 0:
            name = element.get(f"{{{XAML_NS}}}Name") or element.get("Name")
            if name:
                full_type = resolve_type(uri, local, uri_clr)
                if full_type is None:
                    warnings.append(f"{path}: 无法解析 {local}（xmlns={uri}）的类型，已跳过控件 {name}")
                elif name in seen:
                    warnings.append(f"{path}: x:Name 重复：{name}，已跳过")
                else:
                    seen.add(name)
                    found.append((name, full_type))

        for child in element:
            walk(child, skip_depth)

    walk(root, 0)
    return x_class, found, warnings


def find_expression(name: str, full_type: str) -> str:
    """FindControl 约束为 Control，非 Control 的命名对象改用 NameScope 查找。"""
    if full_type.startswith("Avalonia.Media.") or full_type.startswith("Avalonia.Controls.Documents."):
        return f'NameScopeExtensions.Find(Of {full_type})(Me, "{name}")'
    return f'FindControl(Of {full_type})("{name}")'


def render(x_class: str, controls: list[tuple[str, str]], source: str) -> str:
    parts = x_class.split(".")
    namespace = ".".join(parts[:-1])
    class_name = parts[-1]

    lines = [
        "' ------------------------------------------------------------------------------",
        "' <auto-generated>",
        f"'   由 tools/XamlGen/gen_fields.py 根据 {source} 生成，请勿手动修改。",
        "' </auto-generated>",
        "' ------------------------------------------------------------------------------",
        "Option Strict Off",
        "Option Explicit On",
        "",
        "Imports Avalonia",
        "Imports Avalonia.Controls",
        "Imports Avalonia.Markup.Xaml",
        "",
    ]
    if namespace:
        lines.append(f"Namespace Global.{namespace}")
        lines.append("")

    lines.append(f"    Partial Public Class {class_name}")
    lines.append("")

    for name, full_type in controls:
        lines.append(f"        Friend WithEvents {vb_identifier(name)} As {full_type}")
    lines.append("")

    lines.append("        Private Sub InitControls()")
    lines.append("            AvaloniaXamlLoader.Load(Me)")
    for name, full_type in controls:
        lines.append(f"            {vb_identifier(name)} = {find_expression(name, full_type)}")
        lines.append(
            f"            If {vb_identifier(name)} Is Nothing Then "
            f"Throw New InvalidOperationException(\"找不到控件：{name}\")"
        )
    lines.append("        End Sub")
    lines.append("")
    lines.append("    End Class")

    if namespace:
        lines.append("")
        lines.append("End Namespace")

    lines.append("")
    return "\n".join(lines)


def generate(project: Path, check: bool) -> int:
    generated_dir = project / "Generated"
    outputs: dict[Path, str] = {}
    warnings: list[str] = []

    for axaml in sorted(project.rglob("*.axaml")):
        rel = axaml.relative_to(project)
        if rel.parts[0] in ("Generated", "bin", "obj"):
            continue
        x_class, controls, file_warnings = scan_names(axaml)
        warnings.extend(file_warnings)
        if not x_class or not controls:
            continue
        filename = pascal_case(x_class.split(".")[-1]) + ".g.vb"
        target = generated_dir / filename
        if target in outputs:
            warnings.append(f"{axaml}: 生成文件名冲突：{target.name}")
        outputs[target] = render(x_class, controls, str(rel))

    for warning in warnings:
        print(f"warning: {warning}", file=sys.stderr)

    if check:
        drift = 0
        for target, content in outputs.items():
            if not target.exists():
                print(f"missing: {target}", file=sys.stderr)
                drift += 1
            elif target.read_text(encoding="utf-8") != content:
                print(f"outdated: {target}", file=sys.stderr)
                drift += 1
        if drift:
            print(f"{drift} 个生成文件与 XAML 不一致，请运行 gen_fields.py 重新生成。", file=sys.stderr)
            return 1
        print(f"OK：{len(outputs)} 个生成文件均为最新。")
        return 0

    generated_dir.mkdir(parents=True, exist_ok=True)
    written = 0
    for target, content in outputs.items():
        old = target.read_text(encoding="utf-8") if target.exists() else None
        if old != content:
            target.write_text(content, encoding="utf-8")
            written += 1
    print(f"已生成 {len(outputs)} 个文件（更新 {written} 个）。")
    return 0


def main() -> int:
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=root / "PCL")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    if not args.project.is_dir():
        print(f"项目目录不存在：{args.project}", file=sys.stderr)
        return 2
    return generate(args.project, args.check)


if __name__ == "__main__":
    raise SystemExit(main())
