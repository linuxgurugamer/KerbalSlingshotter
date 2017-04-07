


set H=R:\KSP_1.2.2_dev
echo %H%


copy KerbalSlingshotter\KerbalSlingshotter\bin\%1\KerbalSlingshotter.dll GameData\SlingShotter\Plugins
xcopy /E /Y GameData\Slingshotter %H%\GameData\Slingshotter

