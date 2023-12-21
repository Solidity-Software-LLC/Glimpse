## Overview

Glimpse is a modern looking and familiar Windows 11-like panel for XFCE.  It was written to provide a high quality alternative to existing panels and to increase developer accessibility by leveraging modern tools and programming paradigms.  Its goal is to be an all-in-one package that minimizes dependencies on other libraries and components.

## Installation

1. Run ```glimpse install```.
2. Installation will do the following (see install.sh):
   * Copies the Glimpse binary into the ~/.local/bin/ directory.
   * Replaces the xfce-panel with glimpse via xfconf-query.
   * Updates the SUPER_L shortcut to open the Glimpse start menu instead of the whisker menu.
   * Kills the XFCE panel process.
   * Removes the ayatana-indicator-application package.  Glimpse handles status notifier items itself.
   * NOTE: You need to save a new X session if you're not using the default one.
