# AppImage runtime 与 appimagetool（GPL-2.0）

Linux AppImage 发行包由 AppImage 项目的 **appimagetool** 与 **AppImage type-2 runtime** 生成。
其中 AppImage type-2 runtime 以 GPL-2.0 许可发布，并嵌入在生成的 `.AppImage` 产物中；完整许可文本见
同目录 `GPL-2.0.txt`。

- AppImage 项目：https://github.com/AppImage/AppImageKit
- runtime 与 appimagetool 的源码：https://github.com/AppImage/AppImageKit/releases
- 本项目的 AppImage 仅把该 runtime 作为打包运行环境使用，不修改其代码。根据 GPL-2.0，runtime 的源码可从上述
  地址获取；如需对应版本的源码，也可联系本项目维护者。

The Linux AppImage is produced with appimagetool and the AppImage type-2 runtime from the AppImage project.
The type-2 runtime is licensed under GPL-2.0 and is embedded in the generated `.AppImage`. The full license text
is provided in `GPL-2.0.txt`; the corresponding runtime source is available from the AppImage project.
