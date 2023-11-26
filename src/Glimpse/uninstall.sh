#!/bin/bash

echo
echo "### Remove binary from ~/local/bin/"
rm ~/.local/bin/Glimpse

echo
echo "### Remove autostart desktop file"
rm ~/.config/autostart/Glimpse.desktop

echo
echo "### Reset SUPER_L shortcut"
xfconf-query \
	-c xfce4-keyboard-shortcuts \
	-p "/commands/custom/<Primary>Escape" \
	-r

echo
echo "### Enabling XFCE panel"
echo chmod 755 /usr/bin/xfce4-panel
sudo chmod 755 /usr/bin/xfce4-panel

echo
echo "### Installing ayatana-indicator-application"
sudo apt-get install ayatana-indicator-application

echo
echo "Uninstall complete -- The XFCE panel will load the next time you log in"
