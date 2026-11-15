# ScreenMirror

Приложение для Windows, которое принимает экран и звук iPhone через AirPlay.
Открываешь ScreenMirror, выбираешь компьютер в **«Повторе экрана»** на iPhone и вводишь код из окна приложения.
На iPhone ничего устанавливать не нужно.

## Установка

1. Открой **Releases** этого репозитория.
2. Скачай `ScreenMirror-Setup-1.0.0-x64.exe` и запусти установщик.
3. Открой ScreenMirror через меню «Пуск» или ярлык на рабочем столе.
4. Подключи iPhone и компьютер к одной локальной сети.
5. На iPhone: **Пункт управления → Повтор экрана → ScreenMirror ИМЯ-ПК**.

Если Windows запросит доступ для `uxplay.exe`, разреши его для своей частной домашней сети.
MSYS2, PowerShell и инструменты сборки на компьютере пользователя не требуются.
Сам установщик содержит необходимые библиотеки; после скачивания ему не нужен интернет.

Требования: Windows 10/11 x64, .NET Framework 4.8, локальная сеть с доступом между устройствами.
На Windows on ARM работа не проверена. Компьютер может подключаться к роутеру по кабелю.
Защищённое DRM-видео может не транслироваться. Управление iPhone с компьютера не поддерживается.

## Управление

- После запуска приёмник включается автоматически.
- Во время трансляции крестик отключает телефон и возвращает в хаб.
- В хабе крестик закрывает приложение.
- Средняя верхняя кнопка включает полный экран.
- При изменении размера трансляции мышью перемещается контур; размер применяется после отпускания кнопки. Esc отменяет изменение.
- Минимальная ширина хаба — 920 пикселей.

Журнал находится в `%LOCALAPPDATA%\ScreenMirror`; кнопка «Журнал» открывает эту папку.
Удаление приложения доступно через настройки Windows. Журналы и настройки GStreamer сохраняются отдельно от установочных файлов.
Перед обновлением закрой приложение из хаба. Старую ручную версию из предыдущей установки следует закрыть и пользоваться новым ярлыком.

## Сборка Windows

Автоматическая сборка находится в `.github/workflows/windows.yml`.
Для обычного коммита сборка появляется в **Actions → Windows installer → Artifacts**.
Тег `v1.0.0` запускает сборку и публикует GitHub Release после прохождения проверок.

Для ручной сборки нужны MSYS2 UCRT64, Python 3.11+, .NET Framework compiler и Inno Setup 6:

```bash
pacman -S --needed git python zip ca-certificates mingw-w64-ucrt-x86_64-cmake mingw-w64-ucrt-x86_64-ninja mingw-w64-ucrt-x86_64-gcc mingw-w64-ucrt-x86_64-pkgconf mingw-w64-ucrt-x86_64-openssl mingw-w64-ucrt-x86_64-libplist mingw-w64-ucrt-x86_64-gstreamer mingw-w64-ucrt-x86_64-gst-plugins-base mingw-w64-ucrt-x86_64-gst-plugins-good mingw-w64-ucrt-x86_64-gst-plugins-bad mingw-w64-ucrt-x86_64-gst-libav
bash scripts/build-engine.sh
bash scripts/bundle-runtime.sh
```

Затем в PowerShell из папки репозитория:

```powershell
.\scripts\Build-App.ps1 -Version 1.0.0
& "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" /DAppVersion=1.0.0 installer\ScreenMirror.iss
python scripts/package-release.py 1.0.0
```

Результаты находятся в `dist/`: установщик, `ScreenMirror-source.zip` и `SHA256SUMS.txt`.
При изменении библиотек сборка автоматически фиксирует их версии, зависимости и соответствующие исходные архивы MSYS2.
Если для любой поставляемой библиотеки не удалось получить исходники, сборка останавливается.

## Проверки

```powershell
python -m unittest discover -s tests -v
.\scripts\Test-App.ps1 -AppDirectory (Join-Path $PWD 'dist\payload')
```

Windows CI проверяет запуск движка, доступность плагинов H.264 и Direct3D11, запуск GUI,
тихую установку и удаление. Реальное подключение iPhone и аппаратное декодирование требуют отдельной проверки на физическом компьютере.
Установщик не подписан сертификатом издателя.

## Лицензии и исходники

Оболочка: MIT, см. `LICENSE`.
Движок: [UxPlay](https://github.com/FDH2/UxPlay), GPL-3.0,
зафиксирован commit `05b0275cb1b726592700e47a6e0307544b3d3a52`.
GStreamer и другие динамические библиотеки сохраняют собственные лицензии.

В установленной папке находятся `THIRD-PARTY-NOTICES.txt`, `licenses/`, `runtime-versions.txt` и `runtime-sources.json`.
`ScreenMirror-source.zip`, распространяемый вместе с установщиком, содержит исходники приложения и UxPlay,
точные исходные архивы runtime-пакетов MSYS2, рецепты сборки и сведения об их происхождении.
Системные компоненты Windows и .NET Framework в установщик не включены.
