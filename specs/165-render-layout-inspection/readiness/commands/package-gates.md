fake.sh status: not present in this checkout; running direct repository substitutes.

$ dotnet build FS.GG.Rendering.slnx --no-restore
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Debug/net10.0/FS.GG.UI.Scene.dll
  Testing -> /home/developer/projects/FS.GG.Rendering/src/Testing/bin/Debug/net10.0/FS.GG.UI.Testing.dll
  Scene.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Scene.Tests/bin/Debug/net10.0/Scene.Tests.dll
  Layout -> /home/developer/projects/FS.GG.Rendering/src/Layout/bin/Debug/net10.0/FS.GG.UI.Layout.dll
  DesignSystem -> /home/developer/projects/FS.GG.Rendering/src/DesignSystem/bin/Debug/net10.0/FS.GG.UI.DesignSystem.dll
  KeyboardInput -> /home/developer/projects/FS.GG.Rendering/src/KeyboardInput/bin/Debug/net10.0/FS.GG.UI.KeyboardInput.dll
  Color -> /home/developer/projects/FS.GG.Rendering/src/Color/bin/Debug/net10.0/FS.GG.UI.Color.dll
  Testing.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Testing.Tests/bin/Debug/net10.0/Testing.Tests.dll
  Themes.AntDesign -> /home/developer/projects/FS.GG.Rendering/src/Themes.AntDesign/bin/Debug/net10.0/FS.GG.UI.Themes.AntDesign.dll
  Color.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Color.Tests/bin/Debug/net10.0/Color.Tests.dll
  Themes.Default -> /home/developer/projects/FS.GG.Rendering/src/Themes.Default/bin/Debug/net10.0/FS.GG.UI.Themes.Default.dll
  SkiaViewer -> /home/developer/projects/FS.GG.Rendering/src/SkiaViewer/bin/Debug/net10.0/FS.GG.UI.SkiaViewer.dll
  KeyboardInput.Tests -> /home/developer/projects/FS.GG.Rendering/tests/KeyboardInput.Tests/bin/Debug/net10.0/KeyboardInput.Tests.dll
  Controls -> /home/developer/projects/FS.GG.Rendering/src/Controls/bin/Debug/net10.0/FS.GG.UI.Controls.dll
  Elmish -> /home/developer/projects/FS.GG.Rendering/src/Elmish/bin/Debug/net10.0/FS.GG.UI.Elmish.dll
  Layout.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Layout.Tests/bin/Debug/net10.0/Layout.Tests.dll
  Smoke.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Smoke.Tests/bin/Debug/net10.0/Smoke.Tests.dll
  Controls.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Controls.Tests/bin/Debug/net10.0/Controls.Tests.dll
  Lib.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Lib.Tests/bin/Debug/net10.0/Lib.Tests.dll
  Input -> /home/developer/projects/FS.GG.Rendering/src/Input/bin/Debug/net10.0/FS.GG.UI.Input.dll
  Controls.Elmish -> /home/developer/projects/FS.GG.Rendering/src/Controls.Elmish/bin/Debug/net10.0/FS.GG.UI.Controls.Elmish.dll
  Input.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Input.Tests/bin/Debug/net10.0/Input.Tests.dll
  Rendering.Harness -> /home/developer/projects/FS.GG.Rendering/tests/Rendering.Harness/bin/Debug/net10.0/Rendering.Harness.dll
  SkiaViewer.Tests -> /home/developer/projects/FS.GG.Rendering/tests/SkiaViewer.Tests/bin/Debug/net10.0/SkiaViewer.Tests.dll
  Elmish.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Elmish.Tests/bin/Debug/net10.0/Elmish.Tests.dll
  Rendering.Harness.Tests -> /home/developer/projects/FS.GG.Rendering/tests/Rendering.Harness.Tests/bin/Debug/net10.0/Rendering.Harness.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:03.58

$ dotnet fsi scripts/refresh-surface-baselines.fsx
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Layout.txt (96 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.KeyboardInput.txt (34 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Controls.txt (479 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt (27 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.DesignSystem.txt (44 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Themes.AntDesign.txt (2 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Themes.Default.txt (5 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Elmish.txt (12 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Input.txt (76 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Scene.txt (215 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.SkiaViewer.txt (278 public types)
wrote /home/developer/projects/FS.GG.Rendering/tests/surface-baselines/FS.GG.UI.Testing.txt (134 public types)

$ pack all packable src projects to ~/.local/share/nuget-local
$ dotnet pack src/Color/Color.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  Color -> /home/developer/projects/FS.GG.Rendering/src/Color/bin/Release/net10.0/FS.GG.UI.Color.dll
  The package FS.GG.UI.Color.0.1.26-preview.1 is missing a readme. Go to https://aka.ms/nuget/authoring-best-practices/readme to learn why package readmes are important.
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Color.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Controls.Elmish/Controls.Elmish.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  DesignSystem -> /home/developer/projects/FS.GG.Rendering/src/DesignSystem/bin/Release/net10.0/FS.GG.UI.DesignSystem.dll
  KeyboardInput -> /home/developer/projects/FS.GG.Rendering/src/KeyboardInput/bin/Release/net10.0/FS.GG.UI.KeyboardInput.dll
  Layout -> /home/developer/projects/FS.GG.Rendering/src/Layout/bin/Release/net10.0/FS.GG.UI.Layout.dll
  SkiaViewer -> /home/developer/projects/FS.GG.Rendering/src/SkiaViewer/bin/Release/net10.0/FS.GG.UI.SkiaViewer.dll
  Controls -> /home/developer/projects/FS.GG.Rendering/src/Controls/bin/Release/net10.0/FS.GG.UI.Controls.dll
  Controls.Elmish -> /home/developer/projects/FS.GG.Rendering/src/Controls.Elmish/bin/Release/net10.0/FS.GG.UI.Controls.Elmish.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Controls.Elmish.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Controls/Controls.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  Layout -> /home/developer/projects/FS.GG.Rendering/src/Layout/bin/Release/net10.0/FS.GG.UI.Layout.dll
  KeyboardInput -> /home/developer/projects/FS.GG.Rendering/src/KeyboardInput/bin/Release/net10.0/FS.GG.UI.KeyboardInput.dll
  DesignSystem -> /home/developer/projects/FS.GG.Rendering/src/DesignSystem/bin/Release/net10.0/FS.GG.UI.DesignSystem.dll
  Controls -> /home/developer/projects/FS.GG.Rendering/src/Controls/bin/Release/net10.0/FS.GG.UI.Controls.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Controls.0.1.26-preview.1.nupkg'.
$ dotnet pack src/DesignSystem/DesignSystem.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  DesignSystem -> /home/developer/projects/FS.GG.Rendering/src/DesignSystem/bin/Release/net10.0/FS.GG.UI.DesignSystem.dll
  The package FS.GG.UI.DesignSystem.0.1.26-preview.1 is missing a readme. Go to https://aka.ms/nuget/authoring-best-practices/readme to learn why package readmes are important.
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.DesignSystem.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Elmish/Elmish.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  KeyboardInput -> /home/developer/projects/FS.GG.Rendering/src/KeyboardInput/bin/Release/net10.0/FS.GG.UI.KeyboardInput.dll
  SkiaViewer -> /home/developer/projects/FS.GG.Rendering/src/SkiaViewer/bin/Release/net10.0/FS.GG.UI.SkiaViewer.dll
  Elmish -> /home/developer/projects/FS.GG.Rendering/src/Elmish/bin/Release/net10.0/FS.GG.UI.Elmish.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Elmish.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Input/Input.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  KeyboardInput -> /home/developer/projects/FS.GG.Rendering/src/KeyboardInput/bin/Release/net10.0/FS.GG.UI.KeyboardInput.dll
  SkiaViewer -> /home/developer/projects/FS.GG.Rendering/src/SkiaViewer/bin/Release/net10.0/FS.GG.UI.SkiaViewer.dll
  Input -> /home/developer/projects/FS.GG.Rendering/src/Input/bin/Release/net10.0/FS.GG.UI.Input.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Input.0.1.26-preview.1.nupkg'.
$ dotnet pack src/KeyboardInput/KeyboardInput.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  KeyboardInput -> /home/developer/projects/FS.GG.Rendering/src/KeyboardInput/bin/Release/net10.0/FS.GG.UI.KeyboardInput.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.KeyboardInput.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Layout/Layout.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  Layout -> /home/developer/projects/FS.GG.Rendering/src/Layout/bin/Release/net10.0/FS.GG.UI.Layout.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Layout.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Scene/Scene.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Scene.0.1.26-preview.1.nupkg'.
$ dotnet pack src/SkiaViewer/SkiaViewer.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  KeyboardInput -> /home/developer/projects/FS.GG.Rendering/src/KeyboardInput/bin/Release/net10.0/FS.GG.UI.KeyboardInput.dll
  SkiaViewer -> /home/developer/projects/FS.GG.Rendering/src/SkiaViewer/bin/Release/net10.0/FS.GG.UI.SkiaViewer.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.SkiaViewer.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Testing/Testing.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  Testing -> /home/developer/projects/FS.GG.Rendering/src/Testing/bin/Release/net10.0/FS.GG.UI.Testing.dll
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Testing.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Themes.AntDesign/Themes.AntDesign.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  DesignSystem -> /home/developer/projects/FS.GG.Rendering/src/DesignSystem/bin/Release/net10.0/FS.GG.UI.DesignSystem.dll
  Themes.AntDesign -> /home/developer/projects/FS.GG.Rendering/src/Themes.AntDesign/bin/Release/net10.0/FS.GG.UI.Themes.AntDesign.dll
  The package FS.GG.UI.Themes.AntDesign.0.1.26-preview.1 is missing a readme. Go to https://aka.ms/nuget/authoring-best-practices/readme to learn why package readmes are important.
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Themes.AntDesign.0.1.26-preview.1.nupkg'.
$ dotnet pack src/Themes.Default/Themes.Default.fsproj -c Release -o /home/developer/.local/share/nuget-local
  Determining projects to restore...
  All projects are up-to-date for restore.
  Scene -> /home/developer/projects/FS.GG.Rendering/src/Scene/bin/Release/net10.0/FS.GG.UI.Scene.dll
  DesignSystem -> /home/developer/projects/FS.GG.Rendering/src/DesignSystem/bin/Release/net10.0/FS.GG.UI.DesignSystem.dll
  Themes.Default -> /home/developer/projects/FS.GG.Rendering/src/Themes.Default/bin/Release/net10.0/FS.GG.UI.Themes.Default.dll
  The package FS.GG.UI.Themes.Default.0.1.26-preview.1 is missing a readme. Go to https://aka.ms/nuget/authoring-best-practices/readme to learn why package readmes are important.
  Successfully created package '/home/developer/.local/share/nuget-local/FS.GG.UI.Themes.Default.0.1.26-preview.1.nupkg'.
