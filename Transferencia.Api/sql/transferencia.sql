CREATE TABLE IF NOT EXISTS transferencia (
    idtransferencia TEXT PRIMARY KEY,
    idcontacorrente_origem TEXT NOT NULL,
    idcontacorrente_destino TEXT NOT NULL,
    datamovimento TEXT NOT NULL,
    valor REAL NOT NULL
);
