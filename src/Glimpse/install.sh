#!/bin/bash

echo "### Get installation dependencies"
sudo apt-get install xmlstarlet

echo
echo "### Creating ~/.local/bin/"
mkdir ~/.local/bin

echo
echo "### Copy binary to ~/local/bin/"
cp ./Glimpse ~/.local/bin/

echo
echo "### Creating autostart desktop file"
sudo tee ~/.config/autostart/Glimpse.desktop << EOF
[Desktop Entry]
Type=Application
Name=Glimpse
Exec=$HOME/.local/bin/Glimpse
EOF

echo
echo "### Updating SUPER_L shortcut"

shortcutXmlFilePath="$HOME/.config/xfce4/xfconf/xfce-perchannel-xml/xfce4-keyboard-shortcuts.xml"
defaultKeyBindingXPath="//property[@name='commands']/property[@name='default']/property[@name='<Primary>Escape']"
customKeyBindingsXPath="//property[@name='commands']/property[@name='custom']"
customKeyBindingXPath="$customKeyBindingsXPath/property[@name='<Primary>Escape']"

cp $shortcutXmlFilePath $shortcutXmlFilePath.bak

xmlstarlet ed \
	--inplace \
	-u $defaultKeyBindingXPath/@type -v empty \
	-d $defaultKeyBindingXPath/@value \
	-d $customKeyBindingXPath \
	-a "$customKeyBindingsXPath/property[last()]" -t elem -n property \
	-a "$customKeyBindingsXPath/property[last()]" -t attr -n name -v "<Primary>Escape" \
	-a "$customKeyBindingsXPath/property[last()]" -t attr -n type -v "string" \
	-a "$customKeyBindingsXPath/property[last()]" -t attr -n value -v "gdbus call --session --dest org.solidity-software-llc.glimpse --object-path /org/solidity_software_llc/glimpse --method org.gtk.Actions.Activate \"OpenStartMenu\" [] {}" \
	$shortcutXmlFilePath

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
