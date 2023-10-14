Start Menu
------------
* Add context menu with useful actions to taskbar icon
* Hover descriptions for apps
* Pinning to taskbar doesn't update the taskbar

Taskbar
------------
* Calendar

System Tray
------------
* Toggles in system tray context menus
* Scan for StatusNotifierItems that don't register themselves automatically
* I thought GPick had a system tray icon

Packaging
-----------
* Native compilation
* CI/CD pipeline
* DEB package

Technical
------------
* Make all widgets use CSS where possible
* Error handling / logging
* Desktop file matching improvements
  * Look at this algorithm: https://invent.kde.org/plasma/plasma-workspace/-/blob/master/libtaskmanager/tasktools.cpp#L159
  * Rewrite rules (thunar <=> xfce4-file-manager)
  * Compare against pinned/opened taskbar apps
