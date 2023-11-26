#!/bin/bash

echo
echo "### Creating ~/.local/bin/"
mkdir ~/.local/bin

echo
echo "### Copy binary to ~/local/bin/"
cp ./Glimpse ~/.local/bin/

echo
echo "### Creating autostart desktop file"
tee ~/.config/autostart/Glimpse.desktop << EOF
[Desktop Entry]
Type=Application
Name=Glimpse
Exec=$HOME/.local/bin/Glimpse
EOF
chmod 664 ~/.config/autostart/Glimpse.desktop

echo
echo "### Updating SUPER_L shortcut"
xfconf-query \
	-c xfce4-keyboard-shortcuts \
	-p "/commands/custom/<Primary>Escape" \
	-s "gdbus call --session --dest org.solidity-software-llc.glimpse --object-path /org/solidity_software_llc/glimpse --method org.gtk.Actions.Activate \"OpenStartMenu\" [] {}"

echo
echo "### Disabling XFCE panel"
echo chmod a-rwx /usr/bin/xfce4-panel
sudo chmod a-rwx /usr/bin/xfce4-panel

echo
echo "### Stopping XFCE panel"
pkill -9 xfce4-panel

echo
echo "### Removing ayatana-indicator-application"
sudo apt-get purge ayatana-indicator-application
pkill -9 ayatana

echo
echo "### Installation complete"
