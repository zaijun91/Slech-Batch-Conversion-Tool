# MHT 到 PDF 转换器 (Slech 批量转换工具)

一个使用 WPF 和 .NET 8 构建的 Windows 桌面应用程序，用于批量将 MHT/MHTML 文件转换为 PDF 格式。转换过程利用 Microsoft Edge WebView2 运行时来保留原始布局、图片、样式和链接。

## Features

*   Batch conversion of MHT/MHTML files.
*   Preserves original layout, images, CSS, and hyperlinks.
*   Customizable output path and file naming.
*   Progress feedback during conversion.
*   Error logging for failed conversions.
*   Drag and drop support for adding files/folders.
*   Editable output file names directly in the list.
*   Option to automatically overwrite existing PDF files.

## Prerequisites

*   Windows 10/11 (x64)
*   .NET 8 Desktop Runtime (The installer will prompt if needed, but this app is framework-dependent)
*   Microsoft Edge WebView2 Runtime (The included Inno Setup installer will attempt to install this automatically if missing).

## Building from Source

1.  Clone the repository.
2.  Open the solution/project in Visual Studio or use the .NET CLI.
3.  Build the solution (Debug or Release).

## Packaging (using Inno Setup)

1.  Build the project in `Release` configuration (`dotnet publish -c Release --self-contained false -r win-x64`).
2.  Download the [WebView2 Evergreen Standalone Installer (x64)](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section) and place it as `WebView2Runtime.exe` in the `Packaging/Resources` directory.
3.  Place an application icon named `app_icon.ico` in `Packaging/Resources`.
4.  Install [Inno Setup 6](https://jrsoftware.org/isinfo.php).
5.  Compile the `Packaging/MhtToPdfConverter_Setup.iss` script using Inno Setup Compiler (ISCC.exe). The output installer will be in `Packaging/Output`.

## Usage

Run the `MhtToPdfConverter.exe` or install using the generated setup file. Use the GUI to add files/folders, select an output directory, and start the conversion.

## 联系方式 (Contact)

如有问题或建议，可以通过以下方式联系作者：

![微信二维码](docs/images/wechat_qr.png)
