Estructuras JSON para GET (listados)

- GET /api/distribucion
  - [{
      "id": 1,
      "estado": true,
      "asientos": [{ "id": 10, "fila": 1, "columna": 2, "estado": true }, ...]
    }]

- GET /api/pasajes
  - [{
      "id": 1,
      "fechaHora": "2026-08-05 10:00",
      "monto": 50,
      "movil": "1234",
      "estado": true,
      "destino": "Terminal",
      "asiento": { "id": 10, "fila": 1, "columna": 2, "estado": true },
      "cliente": { "id": 5, "nombreCompleto": "Juan", "ci": "123", "telefono": "987" }
    }]

- GET /api/encomienda
  - [{
      "id": 1,
      "contenido": "Caja",
      "fechaRecepcion": "2026-08-04",
      "fechaEntrega": "2026-08-05",
      "monto": 20.5,
      "numero": "E-001",
      "estado": true,
      "clienteRemitente": { "id": 2, "nombreCompleto": "Pedro", "ci": "456" },
      "clienteConsignatario": { "id": 3, "nombreCompleto": "Ana", "ci": "789" },
      "usuario": { "id": 1, "usuario": "admin", "puntoVentaId": 1, "rolId": 1 }
    }]

- GET /api/horarios
  - [{
      "id": 1,
      "fecha": "2026-08-05",
      "hora": "10:00",
      "estado": true,
      "ruta": { "id": 1, "dias": "Lun-Vie", "tarifa": 50, "destinos": [{ "id": 1, "esOrigen": true, "orden": 0, "puntoVenta": { "id": 1, "nombre": "Central", "direccion": "..." } }] },
      "vehiculo": { "id": 2, "placa": "ABC-123", "movil": "22", "conductor": { "id": 4, "nombres": "Luis" } }
    }]

- GET /api/vehiculos
  - [{
      "id": 2,
      "movil": "22",
      "placa": "ABC-123",
      "marca": "Marca",
      "modelo": "Modelo",
      "estado": true,
      "conductor": { "id": 4, "nombres": "Luis" },
      "propietario": { "id": 6, "nombres": "Marco" },
      "distribucion": { "id": 3, "estado": true, "asientos": [{ "id": 10, "fila": 1, "columna": 2 }] }
    }]

- GET /api/clientes
  - [{ "id": 5, "nombreCompleto": "Juan", "ci": "123", "telefono": "987", "estado": true }]

Ejemplos de POST (cuerpos esperados)

- POST /api/pasajes
  - {
      "clienteId": 5,
      "horarioId": 2,
      "asientoId": 10,
      "monto": 50,
      "movil": "1234",
      "destino": "Terminal",
      "usuarioId": 1
    }

- POST /api/encomienda
  - {
      "contenido": "Caja",
      "fechaRecepcion": "2026-08-04",
      "fechaEntrega": "2026-08-05",
      "monto": 20.5,
      "numero": "E-001",
      "clienteRemitenteId": 2,
      "clienteConsignatarioId": 3,
      "usuarioId": 1
    }

- POST /api/vehiculos
  - {
      "movil": "22",
      "placa": "ABC-123",
      "marca": "Marca",
      "modelo": "Modelo",
      "conductorId": 4,
      "propietarioId": 6,
      "distribucionId": 3
    }

- POST /api/clientes
  - {
      "nombreCompleto": "Juan",
      "ci": "123",
      "telefono": "987"
    }


Notas:
- Los GET list devuelven listas proyectadas con las propiedades mínimas necesarias para el front.
- Para POST use los nombres de campo como en los modelos (ejemplo: "clienteId", "asientoId").
- Si necesita más campos para editar/visualizar en detalle, puedo añadir endpoints GET por id con más relaciones.
