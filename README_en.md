<h1 align="center">KeiChat — A Modern WPF Desktop Instant Messaging System Built on .NET 9</h1>

<p align="center">
  <strong> Multi-Process Architecture · MMF Zero-Copy Communication · Strict MVVM Design</strong>
</p>
<p align="center">
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white" alt=".NET 9"></a>
  <a href="https://github.com/dotnet/wpf"><img src="https://img.shields.io/badge/UI-WPF-007ACC?logo=windows&logoColor=white" alt="WPF"></a>
  <a href="https://www.microsoft.com/windows"><img src="https://img.shields.io/badge/OS-Windows%207%2B-0078D4?logo=windows&logoColor=white" alt="Windows 7+"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-MIT-green.svg" alt="License: MIT"></a>
  <a href="https://github.com/macroecho/keichat.wpf/stargazers"><img src="https://img.shields.io/github/stars/macroecho/keichat.wpf?style=social" alt="Stars"></a>
</p>
<p align="center">
  <img src="Images/logo.png" width="100" alt="NeoChat Logo">
</p>

**English** | [简体中文](README.md)

---



## 📖 Introduction

&emsp;&emsp;**KeiChat** is a desktop instant messaging system that started in **2018**. It was originally built on .NET Framework 4.5 + WPF and has served as a core in-house communication tool for day-to-day collaboration. After years of production validation and architectural evolution, the project migrated across .NET Framework 4.5 → .NET Core → .NET 5/6/8, and finally completed a full modernization upgrade in the .NET 9 era. Today it is officially open-sourced and given back to the community.

&emsp;&emsp;Over these eight years, **KeiChat** has evolved alongside the continuous growth of internal business. It gradually grew from a basic internal utility that covered only fundamental messaging needs into a desktop IM solution with a mature architecture and outstanding performance. Every architectural decision came from hands-on experience in real high-concurrency scenarios.

&emsp;&emsp;In terms of architecture, **KeiChat** draws heavily on the best practices of mainstream IM products. It adopts a **multi-process architecture** that fully decouples message communication from UI rendering, and uses <b>MemoryMappedFile (MMF)</b> to achieve **zero-copy data transfer** between processes. This guarantees a fluid, highly responsive UI while providing a solid foundation for high-concurrency message processing, large file transfer, and real-time notifications.

&emsp;&emsp;For real-time communication, **KeiChat** builds its messaging layer on **native Socket + a custom binary protocol + SSL/TLS encryption** instead of high-level abstractions. It supports a complete IM feature matrix — text, stickers, images, videos, files, contact cards, forwarding, recall, favorites — and covers core scenarios such as one-to-one chat, group chat, contact management, and Moments. It is an ideal foundation for building enterprise-grade internal communication systems.

> This project can serve both as a **learning reference for WPF MVVM best practices** and as a **codebase for secondary development of a high-performance desktop IM client**.

#### Latest Version · Modern UI (.NET 9)

Fully rebuilt on .NET 9: multiple theme modes, multi-process architecture, zero-copy communication, and an extremely smooth experience.

![Banner](Images/main-dark.png)



#### 2018 · Early Version (.NET Framework 4.5)

Classic WPF-style UI that satisfied the team's basic instant messaging needs.

![Banner](Images/history.png)

---



## ✨ Architectural Highlights

### 1. 🎖️ Strict MVVM and UI Decoupling

The project adopts the **C# WPF MVVM (Model-View-ViewModel)** pattern to completely separate the UI from business logic:

- **Data-driven UI**: the interface updates dynamically through `INotifyPropertyChanged` and `ICommand`, with no need to manipulate controls manually.
- **High testability**: business logic lives in the ViewModel and can be unit-tested without the UI.
- **Code reuse**: core business logic can be migrated seamlessly to other .NET platforms (such as MAUI and Avalonia).

### 2. ⚡ Multi-Process Architecture: UI Isolated from the Communication Process

To eliminate UI freezes and blocking in real-time communication, the project adopts a multi-process architecture:

| Process | Responsibility |
| -------------------- | --------------------------------------------------- |
| **UI rendering process** | UI presentation, animations, input interaction, and session management |
| **Communication process** | Socket connections, message send/receive, friend/group synchronization, push notifications |
| **File process** | Image processing, video processing, file transfer |
| **Image/video media process** | Browsing images and playing videos |

**Benefits**:

- The UI process always stays lightweight and maintains a silky-smooth **60 FPS** even during message floods.
- Crashes or restarts of the communication, file, or media processes do not affect the stability of the UI process.

#### How the Processes Collaborate

- **Socket receiving thread** → receives binary packets from the server, validates and deserializes them.
- **Message dispatching thread** → classifies messages by conversation and writes them into the corresponding MMF region.
- **UI process** → listens for MMF change events, reads messages, and updates the ViewModel.
- **Sending flow** → UI → MMF → communication process → serialization → SSL Socket → server.

![Architecture diagram](Images/process-architecture.jpg)

### 3. 🛰️ Real-Time Communication

**KeiChat**'s real-time messaging system is built entirely on **native Socket**, rather than HTTP long polling or high-level wrapped protocols.

#### Custom Binary Packet Protocol

To reduce bandwidth usage and improve parsing efficiency, a lightweight binary protocol was designed:

| Field | Description |
| --------------- | -------------------------------------------- |
| `Version` | Protocol version number |
| `SerializeType` | Serialization type |
| `PacketType` | Packet type (one-to-one chat / group chat / heartbeat) |
| `SequenceId` | Packet sequence number, used for de-duplication and ordering |
| `BodyLength` | Length of the packet body |
| `Body` | Serialized message payload (MemoryPack / Protobuf) |
| `Checksum` | CRC32 checksum to protect against data corruption |

![Packet structure](Images/socket-pack.jpg)

#### SSL/TLS Encrypted Transport

- Wrap the raw Socket with `SslStream` to achieve end-to-end encryption (TLS 1.2+).
- The client validates the server certificate to prevent man-in-the-middle attacks.
- All messages and signaling data travel through the encrypted channel.

![Encrypted transport architecture](Images/socket-ssl.jpg)

### 4. 🧾 Data Synchronization Design

**KeiChat** needs to fetch large amounts of data from the business server: **user list, friend list, group list, group members**, and so on. Pulling everything in full every time causes several problems:

- **Wasted bandwidth**: the client already caches most of the data locally, so repeated downloads are expensive.
- **Slow response**: with a large dataset, the first screen takes a long time to load.
- **High server load**: a large number of redundant queries consume server resources.

To address these issues, we adopt an **incremental sync** strategy. Its core goal is: pull everything once on first launch and cache it in the local SQLite database; afterwards, on every startup or pull-to-refresh, synchronize only the data that has been **added or modified** since the last successful sync, minimizing bandwidth usage, improving response speed, and reducing server pressure.

Each time the client initiates a sync request, it carries the **timestamp of the last successful sync**. Based on it, the server returns only the data changed after that timestamp. The client persists the incremental results into its local database (overwriting or inserting) and thus stays consistent with the server. The whole mechanism can be summarized as **three core elements**:

- **Sync anchor**: the point in time the client has synced up to, i.e. the `Timestamp` of the last successful sync.
- **Changed data**: the server returns only records where `update_time > anchor` (both new and modified).
- **Local persistence**: incremental data is written to SQLite via upsert, and the cache stays valid for the long term.

> 📌 **In a nutshell**: the client uses `anchor` to remember "where I have synced up to", the server returns only the changes after that point, and the client idempotently persists them within a transaction and updates the anchor — forming an efficient, reliable, resumable incremental sync loop.

![Incremental data sync sequence diagram](Images/data_sync_flowchart.jpg)

![SQLite](Images/data_sync_db.jpg)

### 5. 🔒 HTTPS Security

To defend against transport-layer Man-in-the-Middle (MITM) attacks, the client enforces a strict **certificate pinning (SSL/TLS pinning)** policy for HTTPS communication. The implementation is as follows:

1. **Preset trust anchor**: the hash of the server's default X.509 certificate is hard-coded into the client resources.
2. **Strict validation during handshake**: in addition to system-level CA chain verification, the TLS handshake performs an extra certificate fingerprint comparison and accepts only a server certificate that exactly matches the pinned information.
3. **Defense effect**: mainstream packet-capturing proxy tools rely on dynamically issuing forged certificates, whose fingerprints cannot match the trust anchor embedded in the client, so the handshake is actively aborted (`SSLHandshakeException`). This effectively eliminates the risk of traffic decryption, request tampering, and sensitive information leakage caused by tricking users into installing a proxy CA certificate.

---



## 🎯 Feature Overview

### 💬 Chat

| Feature | Description |
|---|---|
| 📝 **Text messages** | Up to 800 characters per message; supports default emojis, automatic link and phone number detection |
| 😊 **Sticker messages** | Send favorite animated stickers; received stickers can be added to your own sticker list |
| 🖼️ **Image messages** | Up to 9 images at a time; supports compression, resumable upload, viewing the original image, and saving |
| 🎬 **Video messages** | 1 video at a time; supports resumable upload, upload progress, and status display |
| 📁 **File messages** | 1 file at a time; supports resumable upload, upload progress, and status display |
| 📇 **Contact card messages** | Send a contact's profile card; received cards can be viewed in detail or used to add the person as a friend |
| ↪️ **Forward messages** | Forward individual messages or forward them as a merged bundle to multiple contacts |
| ↩️ **Recall messages** | Can be recalled within 2 minutes after sending; text messages can be re-edited and resent within 5 minutes of recall |
| ⭐ **Favorite messages** | Save received messages to your favorites list and forward them from favorites to other contacts |
| 🔔 **New message alerts** | Numeric badge counters in the conversation list; pop-up toast notifications in the taskbar |
| 📊 **Message status** | Shows sending status/progress; errors can be inspected when sending fails |
| 🔍 **Search chat history** | Fuzzy search across all local chat history to quickly locate text, image, video, file, and other messages |
| 📌 **Pin contacts** | Pin important contacts to the top of the chat list with highlighted reminders |
| 🔕 **Do Not Disturb** | Once enabled, no notifications are received |
| 🗑️ **Clear chat history** | Clear all local chat history of a conversation |


### 👨‍👨‍👧 Group Chat

| Feature | Description |
|---|---|
| ➕ **Create a group** | Select contacts to create a group; set the group avatar/name (a tiled avatar is generated by default); up to 2,000 members (fan-out-on-read model) |
| 🚪 **Join a group** | Join via invitation, group card, or by searching the group number/name |
| 📢 **Group announcement** | Owners/admins can publish announcements; members can view them; supports deletion and editing |
| 📇 **Group card** | Share/forward the group card, and others can join the group directly from the card |
| 🙍‍♂️ **Member management** | Owners/admins can remove members and mute specific members |
| ✏️ **Set group name** | Owners/admins can rename the group and all members are notified afterwards |
| 🖼️ **Set group avatar** | Owners/admins can set the group avatar |
| 📝 **Set group description** | Owners/admins can set a description shown in the group details |
| 🔑 **Set admins** | The owner can add or remove admins |
| 🚫 **Mute settings** | Mute all members or specific members; when all members are muted, only the owner/admins can speak |
| 📋 **Join requests** | When review is enabled, requests from new members appear in a list and must be handled before they can join |
| 🔐 **Join methods** | Three modes: anyone can join / nobody can join / approval required |
| 🔎 **Searchability** | Controls how the group can be found: not searchable / by group number only / by group name or keyword |
| 🤝 **Transfer ownership** | The owner can transfer the group to one of its members |
| 🏃 **Leave a group** | Members can leave and choose whether to keep or clear their chat history beforehand |
| 💥 **Disband a group** | The owner can disband the group and all members will be removed |

### **🙍‍♂️** Contacts

| Feature | Description |
|---|---|
| ➕ **New friends** | Search by email / phone number / account to send a friend request; the friendship takes effect after the other party accepts |
| 🏷️ **Tags** | Assign category tags to contacts for easier management |
| 📋 **Friend profile** | View friend info, set remarks and friend permissions, preview images/videos and Moments thumbnails |
| 🚫 **Block** | Once blocked, the other party cannot view your Moments posts or send you messages |
| 🗑️ **Delete a contact** | After deletion, neither side can view the other's Moments posts or send messages |

### 🌐 Moments

| Feature | Description |
|---|---|
| 📤 **Post** | Publish text, emojis, images, and videos, with a selectable visibility scope |
| 👍 **Like** | Like and un-like posts |
| 💬 **Comment** | Comment, reply to comments, and delete comments |
| 👀 **Browse** | Preview posts in the Moments feed, like and comment directly |

### ⚙️ Others

| Feature | Description |
|---|---|
| ⭐ **Favorites** | Save text/voice/image/video messages from chats as well as images/videos from Moments; supports deletion and forwarding |
| 😊 **Stickers** | Pick images from the album to add to your sticker list, add animated stickers sent by others, and delete or reorder them |
| 🛠️ **Settings** | Set chat ID, nickname, avatar, email, phone number, message notifications, clear all local chat history, appearance, and local storage location |

### 💿 System Experience

- 🌙 **System tray**: closing the window minimizes to the tray; right-click to exit; the icon flashes on new messages
- 🎨 **Modern UI**: built on WPF styles and control templates, with theme switching (dark and light themes)

---



## 🖼️ Screenshots

⚠️ **Asset notice**: the avatars, image messages, and Moments images shown in the screenshots of this repository are collected from the Internet and are used **only to demonstrate product features — they are not content actually generated or collected by this application**. The copyright of these assets belongs to their original authors. If you are a rights holder and believe the usage is inappropriate, please contact us for removal. This project claims no rights over any third-party assets appearing in the screenshots.

### Login and Registration

<p>
  <img src="Images/login.png" width="33%" />
  <img src="Images/register.png" width="33%" />
  <img src="Images/captcha.png" width="33%" />
</p>



### Main Chat Window

![Chat window](Images/main-light.png)

### Friends and Groups

![Friend info](Images/friend.png)

![Group info](Images/group.png)

### Moments and Favorites

![Moments](Images/moment.png)

![Favorites](Images/favorite.png)

### Image Viewer and Video Player

<p>
  <img src="Images/view-image.png" width="49%" />
  <img src="Images/view-video.png" width="49%" />
</p>

---



## 💻 Supported Operating Systems

| OS | Status | Notes |
|---|---|---|
| 🟢 **Windows 7** | ✅ Supported | SP1 or later; the bundled .NET 6 Runtime provides everything required |
| 🟢 **Windows 8.1** | ✅ Supported | Supported out of the box |
| 🟢 **Windows 10** | ✅ Recommended | Best experience, Win10 1903+ |
| 🟢 **Windows 11** | ✅ Recommended | Best experience |
| ❌ **Windows XP** | ❌ Not supported | WPF requires .NET Framework 3.0+; support has been dropped |

> ✅ **KeiChat is backward compatible down to Windows 7.** There is no need to install the .NET Framework manually — the .NET 6 runtime already includes everything WPF requires.

---



## 📦 Download

- #### Windows x64 (Windows 8 and above) [![Download Windows x64](https://img.shields.io/badge/Download-Windows%20x64-purple?style=for-the-badge&logo=windows)](https://github.com/macroecho/keichat.wpf/releases/download/6.0.5/KeiChat-6.0.5-x64-Installer.exe)

- #### Windows 7 x64 [![Download Windows 7 x64](https://img.shields.io/badge/Download-Windows%207%20x64-blue?style=for-the-badge&logo=windows)](https://github.com/macroecho/keichat.wpf/releases/download/6.0.5/KeiChat-6.0.5-win7-x64-Installer.exe)


---



## 🛠️ Tech Stack

- **Framework**: .NET 6 or .NET 9
- **Desktop framework**: WPF
- **Architecture pattern**: MVVM
- **Real-time communication**: native Socket + custom binary protocol + SSL/TLS
- **Serialization**: MemoryPack / Protobuf
- **Inter-process communication**: MemoryMappedFile + Semaphore
- **UI rendering**: WPF control templates + animations
- **Local data storage**: SQLite3 (friends, groups, Moments, chat history)

---



## 🧪 Development Environment Requirements

- Windows 10/11
- .NET 6 SDK or .NET 9 SDK
- Visual Studio 2022+

---



## 🚀 One-Click Server Deployment (Docker Compose)

- [See the Keisoft.Chat.Server deployment guide](https://github.com/macroecho/keisoft.chat.server)

- Configure `App.xaml.cs` as follows:

    ```c# 
    // Gateway address of the KeiChat business service.
    AppSettings.SetGateway("https://demo-chat.example.com");
    
    // Gateway address of the KeiChat file service.
    AppSettings.SetUploadFileGateway("https://demo-chat.example.com/upload-file");
    AppSettings.SetUploadVideoGateway("https://demo-chat.example.com/upload-video");
    AppSettings.SetUploadPictureGateway("https://demo-chat.example.com/upload-image");
    
    // Keep upload-audio and upload-merge.
    AppSettings.SetUploadAudioGateway("https://demo-chat.example.com/upload-audio");
    AppSettings.SetUploadMergeGateway("https://demo-chat.example.com/upload-merge");
    
    // Gateway address of the instant messaging service. Use https (ims) or http (im).
    AppSettings.SetIMServerHost("ims://demo-im.example.com");
    // Specify the port here if it is not 80 or 443.
    // AppSettings.SetIMServerPort(8080);
    ```

---



## ⚖️ Terms of Use and Disclaimer

**1. Lawful use**: Use of this system is subject to the following conditions. You acknowledge and agree to use this system only for lawful purposes and to comply with all applicable laws and regulations, including but not limited to those of your jurisdiction, the place where the activity occurs, and the place where the effects are targeted. You must not use this system to infringe upon the lawful rights and interests of others, to violate the law, or to endanger network security.

> If you breach any of the conditions above, this license terminates immediately and you must stop using this system at once and delete all related copies. You bear all legal liability arising from your breach of this statement, and the maintainers of this project reserve the right to pursue legal action.

**2. Open-source nature**: This project is provided as open source for learning, research, and technical exchange. The developers make no express or implied warranty regarding the completeness, security, stability, or fitness for a particular purpose of this system.

**3. No warranty**: To the maximum extent permitted by law, the developers are not liable for any direct, indirect, incidental, special, or consequential damages arising from the use of or inability to use this system, including but not limited to data loss, business interruption, or other commercial losses.

**4. Assumption of risk**: You understand and agree that all risks arising from the use of this system are borne by you, including but not limited to system vulnerabilities, third-party dependency risks, and runtime environment risks.

**5. Asset notice**: The avatars, image messages, and Moments images shown in the screenshots of this repository are collected from the Internet and are used **only to demonstrate product features — they are not content actually generated or collected by this application**. The copyright of these assets belongs to their original authors. If you are a rights holder and believe the usage is inappropriate, please contact us.

**6. Intellectual property**: The intellectual property rights of this system and its related code and documentation belong to **the original author**. Without written permission, this system may not be used for commercial purposes, nor modified or redistributed without authorization.

---



## 💬 Community

Welcome to the KeiChat technical community to discuss **.NET, WPF, Avalonia, Xamarin, WebRTC, real-time audio/video communication, instant messaging architecture, and multi-process design**, and more 👇

![QQ group](Images/qq.png)

- **QQ group number**: 283798566
- **How to join**: scan the QR code or search the group number in QQ and apply to join
- **Topics**: WPF MVVM architecture, Socket communication, project issue discussions

> 💡 If you don't use QQ, feel free to leave a message in GitHub Issues as well.

---



## 📄 License

MIT License

---

<p align="center">
  <img src="Images/logo.png" width="50" alt="Logo">
  <br/>
  ⭐ If KeiChat helps you learn WPF or build a desktop IM app, please consider giving it a Star!
</p>
