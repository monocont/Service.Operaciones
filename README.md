# Service.Operaciones

Microservicio para la ingesta, validación, procesamiento y persistencia de comprobantes de pago de **Compras** y **Ventas** (Layouts SUNAT SIRE / TXT / CSV) para la plataforma **Monocont**.

---

## 📌 Características
- Carga y procesamiento masivo de archivos de **Compras** (~80 campos) y **Ventas** (~41 campos) en formatos `.txt` y `.csv`.
- Detección de duplicados mediante cálculo de hash **SHA-256**.
- Validaciones completas en pipeline:
  - RUC de archivo vs Empresa activa.
  - Fechas de emisión dentro del periodo contable.
  - Unicidad de `CAR_SUNAT` en memoria y base de datos.
  - Validación de saltos y correlatividad en series numéricas.
- **Unit of Work** con control transaccional explícito (ACID) y **Rollback garantizado** ante fallos para evitar datos inconsistentes o huérfanos.
- Desacoplado de servicios externos en ingesta (proceso autónomo y resiliente).
- Persistencia en **PostgreSQL** con **Entity Framework Core**.

---

## 🏗 Arquitectura y Estructura
```
Service.Operaciones/
├── Service.Operaciones.API/            # CargaController, Swagger y Program.cs
├── Service.Operaciones.Application/    # Commands (CargarCompras, CargarVentas), Queries, UoW y Parsers
├── Service.Operaciones.Domain/         # Entidades (ArchivoCarga, Compra, Venta, ArchivoCargaError)
└── Service.Operaciones.Infrastructure/ # DbContext, UnitOfWork, Repositorios, Parsers TXT/CSV y Scripts SQL
```

---

## ⚙️ Configuración y Variables de Entorno

Archivo: `appsettings.json`

```json
{
  "ConnectionStrings": {
    "OperacionesDb": "Host=localhost;Port=5432;Database=monocont_operaciones;Username=postgres;Password=postgres"
  },
  "ServiceUrls": {
    "Empresa": "http://localhost:5001"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:4200",
      "https://localhost:4200"
    ]
  }
}
```

---

## 🚀 Ejecución

### Puerto por defecto: `5003`

```bash
cd Service.Operaciones.API
dotnet run
```
- **Swagger UI:** `http://localhost:5003/swagger`

---

## 🗄 Base de Datos
Ejecutar los scripts SQL ubicados en `Service.Operaciones.Infrastructure/Script/` en la base de datos `monocont_operaciones`:
- `001_create_schema_operaciones.sql`
- `002_create_archivo_carga.sql`
- `003_create_archivo_carga_error.sql`
- `004_create_compra.sql`
- `005_create_venta.sql`
