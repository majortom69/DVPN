# DVPN - Project Roadmap

DowngradVPN is a Windows VPN application built using WPF that currently supports only OpenVPN.  Below is a high-level roadmap of the planned features and improvements for DowngradVPN.

## Current Status
- **Platform**: Windows (WPF)
- **Current Features**:
  - OpenVPN support
  - App updater(broken rn)
  - User authentication with basic login and password

## Roadmap

### 1. WireGuard Support
**Planned Release**: Q4 2024  
**Details**: Add support for WireGuard, allowing users to choose between OpenVPN and WireGuard protocols for VPN connections. The app will dynamically generate client configurations for both protocols.

### 2. Project Restructuring
**Planned Release**: Q4 2024  
**Details**: The current project structure is pile of shit, and the MVVM pattern is not properly enforced. This phase will focus on:
- **Code Refactoring**: Cleaning up the existing codebase to make it more modular and maintainable.
- **Proper MVVM Implementation**: Ensure that the app strictly follows the MVVM pattern.
  
### 3. Improved Animations, User Interface Enhancements, and Updater Fix
**Planned Release**: Q4 2024  
**Details**: 
- **UI Animations**: Improve the user experience by adding better animations for various components, such as:
  - Button animations (e.g., status changes, hover effects)
  - Loading animations for establishing connections
  - Improved transitions and feedback to make the interface more interactive and visually appealing
- **Updater Fix**: Fix the existing update mechanism to ensure it works recursively. The updated version of the app will:
  - Recursively scan all directories and subdirectories within the current directory for outdated files
  - Download and update the necessary files regardless of their folder location
  - Provide better feedback and status updates to users during the update process(updater window with progress bar)

### 4. Browser Extension for Firefox and Chrome
**Planned Release**: Q1 2025  
**Details**: Develop a browser extension for both Firefox and Chrome to enable seamless VPN activation directly within the browser. Key features:
  - One-click VPN connection and disconnection
  - Auto-connect for specific websites or domains

### 5. Account Management System
**Planned Release**: Q2 2025  
**Details**: Introduce an account management system, expanding beyond the current basic login:
  - Account-based tracking of users
  - Device tracking for connections
  - Ability to view and manage multiple devices connected to the VPN
  - Admin capabilities to monitor user activity, including connection logs and device information

### 6. Mobile Apps for Android and iOS
**Planned Release**: Q2 2025  
**Details**: Expand DowngradVPN to mobile platforms, developing native applications for Android and iOS with the following features:
  - VPN protocol support (OpenVPN and WireGuard)
  - Easy account synchronization with the desktop application
  - Push notifications for connection status and security alerts

## Future Ideas (Beyond 2025)
- Linux and macOS support
- Multi-language support for the app



![image](https://github.com/user-attachments/assets/1cc1027a-da6f-4aeb-835c-3c30584f246c)
