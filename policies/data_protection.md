# Data Protection and Privacy Policy

## 1. Overview

Cobra IDE is a desktop tool for making Python games. This document explains how Cobra handles your data and protects your privacy under South Africa's Protection of Personal Information Act (POPIA).

Cobra works on your local computer. It does not send your data to external servers without your permission.

## 2. Where Data is Stored

All data remains on your local computer:

- **Projects & Code:** Stored only in the folders you choose on your device.
- **IDE Settings:** Saved locally in `%AppData%/Cobra/settings.json`.
- **Project Settings:** Saved locally inside your project folder.
- **Python Environments:** Virtual environments (`.venv`) stay inside your project folder.
- **API Keys:** Any keys you save are encrypted using Windows DPAPI.

## 3. Personal Information

Cobra does not collect your name, email address, phone number, or payment details. No account or registration is required to use the software.

## 4. Why Data is Used

Data is processed on your computer only to:

- Open, edit, save, run, debug, and package your games.
- Remember your preferences (such as window size, theme, and font size).
- Run developer tools you choose (such as Python, pip, and Git).

## 5. Network Connections

Cobra only connects to the network when you use an online feature:

- **Package Installation (pip):** Connects to PyPI to download libraries you request.
- **Git:** Connects to remote repositories when you pull or push code.
- **Optional AI Assistant:** Sends prompts only if you turn on external AI in Settings and provide an API key.

## 6. How to Delete Your Data

You control all your data:

- Delete any project folder to remove its files completely.
- Delete `%AppData%/Cobra` or click **Reset to Defaults** in Settings to clear saved preferences.
- Use **Clean Build Folder** to remove build outputs.

## 7. Your Rights

Under POPIA, you have the right to:

- Know what data is stored and where it is kept.
- Access, edit, or delete your files at any time.
- Turn optional features (like external AI) on or off whenever you want.

## 8. Contact

For security reports or privacy questions, please open an issue in the official project repository.
