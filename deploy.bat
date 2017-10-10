
set H=R:\KSP_1.3.1_dev
echo %H%


copy KerbalSlingshotter\KerbalSlingshotter\bin\%1\KerbalSlingshotter.dll GameData\SlingShotter\Plugins
xcopy /E /Y /i GameData\Slingshotter %H%\GameData\Slingshotter

