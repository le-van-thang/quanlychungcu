# Quanlychungcu (WPF)

<p align="center">
  <img src="docs/logo.png" alt="Logo" height="96"> <!-- 🟡 TODO: thay path logo -->
</p>

> Ứng dụng WPF quản lý chung cư: cư dân, căn hộ, hóa đơn, đăng nhập mạng xã hội, tìm kiếm AI, v.v.  
> **Tech:** .NET Framework 4.7.2 + EF6 + SQL LocalDB.

[![Build](https://img.shields.io/github/actions/workflow/status/le-van-thang/quanlychungcu/build.yml?branch=main)](../../actions)
[![License](https://img.shields.io/badge/license-MIT-informational)](#license)

## 📸 Screenshots
<p align="center">
  <img src="docs/screenshot-dashboard.png" width="700" /> <!-- 🟡 TODO: thêm ảnh -->
</p>

## ✨ Tính năng chính
- Quản lý **Căn hộ / Cư dân / Hóa đơn TM** (CRUD đầy đủ)
- **Đăng nhập OAuth** (Google/Facebook) *(config local)*
- **AI Search** (Gemini/Bing) *(config local)*
- **Biểu đồ / Dashboard** WPF
- Kiến trúc tách lớp + **bộ test** (Unit/Integration)

---

## 🚀 Cài đặt & Chạy

### Yêu cầu
- Windows 10/11, **.NET Framework 4.7.2**
- SQL Server **LocalDB**: `(localdb)\MSSQLLocalDB`
- Visual Studio 2022 (Desktop development with .NET)

### 1) Clone & cấu hình
```bash
git clone https://github.com/le-van-thang/quanlychungcu.git
cd quanlychungcu
