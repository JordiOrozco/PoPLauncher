<div align="center">

  <img src="assets/logo.png" alt="PoPUnturned Launcher Logo" width="180" />

  # 🎮 PoPUnturned Roleplay Launcher

  **Launcher oficial de alto rendimiento y diseño ultra-moderno para el servidor PoPUnturned Roleplay.**

  [![Version](https://img.shields.io/github/v/release/JordiOrozco/PoPLauncher?style=for-the-badge&color=FF3A00&label=RELEASE)](https://github.com/JordiOrozco/PoPLauncher/releases/latest)
  [![.NET 8.0 WPF](https://img.shields.io/badge/.NET-8.0_WPF-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
  [![Platform](https://img.shields.io/badge/PLATFORM-Windows-0078D6?style=for-the-badge&logo=windows)](https://microsoft.com/)
  [![License](https://img.shields.io/badge/LICENSE-MIT-green?style=for-the-badge)](LICENSE)

  <br />

  [📥 Descargar Última Versión (PoPUnturnedSetup.exe)](https://github.com/JordiOrozco/PoPLauncher/releases/latest/download/PoPUnturnedSetup.exe) •
  [🌐 Sitio Web](https://popunturned.com) •
  [💬 Discord](https://discord.popunturned.com)

</div>

---

## 🌟 Características Principales

<table>
  <tr>
    <td width="50%">
      <h3>🚀 Conexión en 1-Clic a Unturned</h3>
      <p>Lanzamiento automático de Unturned a través de la API URI de Steam y conexión directa a la IP e interfaz del servidor de PoPUnturned Roleplay sin necesidad de introducir IP o puerto manualmente.</p>
    </td>
    <td width="50%">
      <h3>📡 Estado de Servidor y Jugadores en Vivo</h3>
      <p>Consulta en tiempo real mediante protocolo A2S (Steam Server Query) del número de jugadores conectados, capacidad del servidor, estado ONLINE/OFFLINE y lista con los nombres de los usuarios activos.</p>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <h3>🔊 Control del Volumen de Vehículos</h3>
      <p>Slider exclusivo en el menú de opciones para modificar directamente la clave <code>Vehicle_Engine_Volume_Multiplier</code> en el archivo <code>Preferences.json</code> de Unturned, ofreciendo control absoluto sobre el ruido del motor.</p>
    </td>
    <td width="50%">
      <h3>🔄 Auto-Updater Integrado</h3>
      <p>Sistema de actualización automática sin intervención manual. Al haber una nueva versión en GitHub Releases, el launcher la detectará, mostrará una barra de progreso animada de descarga y aplicará el nuevo instalador de forma transparente.</p>
    </td>
  </tr>
  <tr>
    <td width="50%">
      <h3>📂 Rutas de Steam Personalizables</h3>
      <p>Selector de carpetas con explorador de archivos de Windows para ubicar Unturned o la carpeta de Mods de Workshop (304930) en discos secundarios o rutas personalizadas (ej. <code>D:\SteamLibrary...</code>).</p>
    </td>
    <td width="50%">
      <h3>🎵 Reproductor de Música Lo-Fi Chill</h3>
      <p>Música de ambiente relajante (libre de copyright) integrada con control de encendido/silenciado rápido en la barra superior y regulador de volumen independiente en las opciones.</p>
    </td>
  </tr>
</table>

---

## 🎨 Diseño Visual y Redes Sociales

El launcher cuenta con una interfaz **Glassmorphism de alta fidelidad** basada en un tema oscuro con tonos carmesí y dorado:

- **Iconografía Oficial Vectorial**: Enlaces interactivos a las redes oficiales con SVG vectoriales nítidos:
  - 💬 **Discord**
  - 🌐 **Sitio Web Oficial**
  - ▶️ **YouTube**
  - 🎵 **TikTok**
  - 𝕏 **Twitter / X**
  - 📸 **Instagram**
- **🧹 Limpiador de Mods**: Botón directo en la sección de opciones para vaciar la caché descargada de la Workshop (ID 304930) y solucionar conflictos de mods corruptos o desactualizados.

---

## 💻 Requisitos del Sistema

- **Sistema Operativo**: Windows 10 / Windows 11 (64-bit)
- **Juego Base**: Unturned instalado en Steam (AppID `304930`)
- **Runtime**: Incluido de forma nativa en el instalador (basado en .NET 8 Desktop Runtime).

---

## 🛠️ Tecnologías Utilizadas

- **Lenguaje**: C# 12
- **Framework**: .NET 8.0 Windows (WPF)
- **Protocolo de Servidor**: Steam A2S Master Server Query (UDP)
- **Serialización**: `System.Text.Json` / `System.Text.Json.Nodes`
- **Audio**: `System.Windows.Media.MediaPlayer` (Lo-Fi Ambient Stream)

---

## 📝 Licencia

Este proyecto está distribuido bajo la licencia MIT. Consulta el archivo [LICENSE](LICENSE) para más detalles.

<div align="center">
  <sub>Desarrollado con ❤️ para la comunidad de <b>PoPUnturned Roleplay</b>.</sub>
</div>
