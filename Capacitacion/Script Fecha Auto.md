###### ***// =================================================***

###### ***// FUNCION DE AUTOMATIZACION EN COLUMNAS ESPECIFICAS***

###### ***// =================================================***

function onEdit(e) {

&nbsp; const range = e.range;

&nbsp; const row = range.getRow();

&nbsp; const col = range.getColumn();

&nbsp; const sheet = e.source.getActiveSheet();

&nbsp; const sheetName = sheet.getName();



&nbsp; // FILTRO DE SEGURIDAD BÁSICO

&nbsp; if (!e.value || row < 3) return;



&nbsp; // CONFIGURACIÓN PERSONALIZADA POR HOJA

&nbsp; const config = {

&nbsp;   "CAJA 26": { 

&nbsp;     disparador: 5, 

&nbsp;     colFecha: 2, 

&nbsp;     colHora: 4, 

&nbsp;     colCod: 17,

&nbsp;     colDespacho: 16,

&nbsp;     colHoraDespacho: 18 },

&nbsp;   "MARZO 2026": { 

&nbsp;     disparador: 4, 

&nbsp;     colFecha: 2, 

&nbsp;     colHora: 3, 

&nbsp;     colCod: 18,

&nbsp;     colDespacho: 17,     

&nbsp;     colHoraDespacho: 19  

&nbsp;   } 

&nbsp; };



&nbsp; const ajustes = config\[sheetName];

&nbsp; if (!ajustes) return;



&nbsp; // LÓGICA PARA REGISTRO DE HORA DE DESPACHO

&nbsp; if (ajustes.colDespacho \&\& col === ajustes.colDespacho) {

&nbsp;   const valorEstado = e.value.toString().toUpperCase();

&nbsp;   

&nbsp;   // Si el estado es "D" (Despachado)

&nbsp;   if (valorEstado === "D") {

&nbsp;     const horaActual = Utilities.formatDate(new Date(), "GMT-5", "HH:mm");

&nbsp;     const celdaHoraS = sheet.getRange(row, ajustes.colHoraDespacho);

&nbsp;     

&nbsp;     // Solo registra la hora si la celda está vacía (evita sobrescribir si cambian de D a D)

&nbsp;     if (celdaHoraS.getValue() === "") {

&nbsp;       celdaHoraS.setValue(horaActual);

&nbsp;       celdaHoraS.setNumberFormat("HH:mm");

&nbsp;     }

&nbsp;     return; // Finaliza para evitar ejecutar las otras lógicas

&nbsp;   }

&nbsp; }



&nbsp; // LÓGICA PARA LIMPIAR CÓDIGO (Ya existente)

&nbsp; if (col === ajustes.colCod) {

&nbsp;   var valorEntrante = e.value.toString();

&nbsp;   if (valorEntrante.indexOf("-") !== -1) {

&nbsp;     var codigoPuro = Number(valorEntrante.split("-")\[0].trim());

&nbsp;     if (!isNaN(codigoPuro)) {

&nbsp;       range.setValue(codigoPuro);

&nbsp;       return; 

&nbsp;     }

&nbsp;   }

&nbsp; }

&nbsp; 

&nbsp; // LÓGICA DE REGISTRO DE FECHA Y HORA DE CREACIÓN (Solo si es la columna disparadora)

&nbsp; if (col !== ajustes.disparador) return;



&nbsp; const now = new Date();



&nbsp; const timezone = Session.getScriptTimeZone();



&nbsp; const fechaPura = Utilities.formatDate(now, timezone, "dd/MM/yyyy");

&nbsp; const horaPura = Utilities.formatDate(now, "GMT-5", "HH:mm");



&nbsp; const colInicio = Math.min(ajustes.colFecha, ajustes.colHora);

&nbsp; const ancho = Math.max(ajustes.colFecha, ajustes.colHora) - colInicio + 1;

&nbsp; const rangoBloque = sheet.getRange(row, colInicio, 1, ancho);

&nbsp; 

&nbsp; const valores = rangoBloque.getValues()\[0];

&nbsp; const formatos = rangoBloque.getNumberFormats()\[0];



&nbsp; const idxFecha = ajustes.colFecha - colInicio;

&nbsp; const idxHora = ajustes.colHora - colInicio;



&nbsp; let huboCambio = false;



&nbsp; if (valores\[idxFecha] === "") {

&nbsp;   valores\[idxFecha] = fechaPura; 

&nbsp;   formatos\[idxFecha] = "dd/mm/yyyy";

&nbsp;   huboCambio = true;

&nbsp; }

&nbsp; if (valores\[idxHora] === "") {

&nbsp;   valores\[idxHora] = horaPura; 

&nbsp;   formatos\[idxHora] = "HH:mm";

&nbsp;   huboCambio = true;

&nbsp; }



&nbsp; if (huboCambio) {

&nbsp;   rangoBloque.setValues(\[valores]);

&nbsp;   rangoBloque.setNumberFormats(\[formatos]);

&nbsp; }

}

