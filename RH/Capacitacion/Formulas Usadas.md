###### ***Columna A (PO)***

**Contador Inteligente:** Genera el número consecutivo automáticamente cuando detecta una fecha en la columna B. No se debe escribir sobre ella



=ARRAYFORMULA(SI(ESBLANCO(B3:B); ; CONTAR.SI.CONJUNTO(B3:B; B3:B; FILA(B3:B); "<="\&FILA(B3:B))))





###### ***Columna E (ZONA)***

**Asignación de Zona:** Busca el barrio de la columna D en la base de datos y trae la zona correspondiente de forma automática para toda la columna.



=ARRAYFORMULA(SI(D2:D=""; ""; BUSCARV(D2:D; 'BARRIO - ZONAS'!$A$2:$B; 2; FALSO)))





###### ***Datos Cliente (DIRECCIÓN)***

**Conexión Externa (2):** Trae la dirección desde la Base de Datos central. El número 2 cambia a 3 (Barrio), 4 (Zona) o 5 (Teléfono) según la columna.



=SI(D3=""; ""; SI.ERROR(BUSCARV(D3; IMPORTRANGE("ID\_LIBRO"; "BASE!B:F"); 2; FALSO); ""))





###### Columna Vehículo

**Vinculación de Placa:** Toma el código del conductor de la columna R y busca la placa asignada en la pestaña de parámetros.



=ARRAYFORMULA(SI(R3:R=""; ""; BUSCARV(R3:R\*1; COD\_VEHICULO!$A:$B; 2; FALSO)))

