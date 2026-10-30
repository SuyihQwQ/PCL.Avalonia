#!/usr/bin/env python3
"""
WPF XAML -> Avalonia XAML 的机械化转换规则。

只处理能可靠文本替换的部分；语义性改动（Storyboard、DropShadowEffect、
AdornerDecorator 等）会在转换后列出来，需要人工处理。

用法：
    python3 tools/XamlGen/convert_rules.py <来源.xaml> <目标.axaml>
    python3 tools/XamlGen/convert_rules.py --report <来源.xaml>
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

PRESENTATION_NS = "http://schemas.microsoft.com/winfx/2006/xaml/presentation"
AVALONIA_NS = "https://github.com/avaloniaui"
ASSEMBLY = "PCL"

# 需要用人工重写的 WPF 专有结构
MANUAL_PATTERNS = [
    (r"<Storyboard\b", "Storyboard 需改为 Transitions / 动画代码"),
    (r"<EventTrigger\b", "EventTrigger 需改为 Transitions 或代码订阅"),
    (r"<DataTrigger\b", "DataTrigger 需改为伪类选择器或绑定逻辑"),
    (r"<Trigger\b", "Trigger 需改为伪类选择器（:pointerover 等）"),
    (r"DropShadowEffect", "DropShadowEffect 需改为 Border.BoxShadow"),
    (r"<AdornerDecorator\b", "AdornerDecorator 需改为 ContentControl"),
    (r"<AdornerLayer\b", "AdornerLayer 在 Avalonia 不存在"),
    (r"WindowStyle\s*=", "WindowStyle 需改为 WindowDecorations / SystemDecorations"),
    (r"AllowsTransparency\s*=", "AllowsTransparency 在 Avalonia 中始终可用，删除即可"),
    (r"RelativeSource\s*=", "RelativeSource 绑定需改为 $self / $parent[Type] / $templateParent"),
    (r"Visibility\s*=", "Visibility 需改为 IsVisible 或绑定到 bool"),
    (r"SnapsToDevicePixels\s*=", "SnapsToDevicePixels 需评估是否改为 UseLayoutRounding"),
    (r"RenderOptions\.", "RenderOptions 在 Avalonia 大多不存在，需逐项处理"),
    (r"TextOptions\.", "TextOptions 在 Avalonia 不存在"),
    (r"<Viewbox\b", "Viewbox 在 Avalonia 中为 Viewbox（确认命名空间）"),
    (r"Grid\.SharedSizeGroup", "SharedSizeGroup 在 Avalonia 不支持"),
    (r"x:Shared\s*=", "x:Shared 在 Avalonia 不支持"),
    (r"<FlowDocument\b", "FlowDocument 需改为 TextBlock / SelectableTextBlock"),
    (r"<Label\b", "Label 语义不同，建议改为 TextBlock / ContentControl"),
]


def convert(text: str) -> tuple[str, list[str]]:
    notes: list[str] = []

    text = text.replace(PRESENTATION_NS, AVALONIA_NS)
    text = text.replace("pack://application:,,,/", f"avares://{ASSEMBLY}/")

    # 简单的 ElementName 绑定 -> 编译绑定简写
    def element_binding(match: re.Match[str]) -> str:
        element, path = match.group(1), match.group(2)
        notes.append(f"ElementName 绑定已转换为 #Element（{element}.{path}）")
        return "{Binding #" + element + "." + path + "}"

    text = re.sub(r"\{Binding\s+ElementName=([A-Za-z_][\w.]*),\s*Path=([\w.\[\]]+)\}", element_binding, text)

    # Visibility 字面量
    text = re.sub(r'\s+Visibility="Visible"', "", text)
    text = re.sub(r'\s+Visibility="(Collapsed|Hidden)"', ' IsVisible="False"', text)

    # Style TargetType -> Selector（仅在单个 Style 标签内、未带 x:Key 的隐式样式）
    def style_selector(match: re.Match[str]) -> str:
        body = match.group(1)
        if "x:Key" in body:
            notes.append("带 x:Key 的 Style 需人工改为 ControlTheme")
            return match.group(0)
        type_match = re.search(r'TargetType="\{x:Type\s+([\w:]+)\}"', body)
        if not type_match:
            type_match = re.search(r'TargetType="([\w:]+)"', body)
        if not type_match:
            return match.group(0)
        type_name = type_match.group(1)
        body = re.sub(r'\s+TargetType="[^"]*"', "", body)
        notes.append(f"隐式 Style 已转换为 Selector=\"{type_name}\"")
        return '<Style Selector="' + type_name + '"' + body + ">"

    text = re.sub(r"<Style(?=[\s/>])([^>]*)>", style_selector, text)

    # Window 自绘相关
    text = re.sub(r'\s+WindowStyle="None"', ' WindowDecorations="None"', text)
    text = re.sub(r'\s+AllowsTransparency="[^"]*"', "", text)

    return text, notes


def report(text: str) -> list[str]:
    findings = []
    for pattern, message in MANUAL_PATTERNS:
        for match in re.finditer(pattern, text):
            line = text.count("\n", 0, match.start()) + 1
            findings.append(f"行 {line}: {message}")
    return findings


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("target", type=Path, nargs="?")
    parser.add_argument("--report", action="store_true", help="只列出需要人工处理的项")
    args = parser.parse_args()

    if not args.source.is_file():
        print(f"文件不存在：{args.source}", file=sys.stderr)
        return 2

    original = args.source.read_text(encoding="utf-8-sig")
    if args.report:
        findings = report(original)
        print("\n".join(findings) if findings else "未发现需要人工处理的 WPF 专有结构。")
        return 0

    if args.target is None:
        print("缺少目标路径（或使用 --report）。", file=sys.stderr)
        return 2

    converted, notes = convert(original)
    args.target.write_text(converted, encoding="utf-8")

    for note in notes:
        print(f"info: {note}", file=sys.stderr)
    findings = report(converted)
    if findings:
        print("仍需人工处理：", file=sys.stderr)
        print("\n".join(findings), file=sys.stderr)
    print(f"已写入 {args.target}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
