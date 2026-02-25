# **_Tercera Implementación de Cambios_**

## 1. Hora Automatica D,P

> ORDEN DE PEDIDOS

- Ir a AppScript

- Actualizar la funcion de Fecha_Hora_Auto:

  ```js
  // =================================================
  // FUNCION DE AUTOMATIZACION EN COLUMNAS ESPECIFICAS
  // =================================================
  function onEdit(e) {
    const range = e.range;
    const row = range.getRow();
    const col = range.getColumn();
    const sheet = e.source.getActiveSheet();
    const sheetName = sheet.getName();

    // FILTRO DE SEGURIDAD BÁSICO
    if (!e.value || row < 3) return;

    // CONFIGURACIÓN PERSONALIZADA POR HOJA
    const config = {
      "CAJA 26": {
        disparador: 5,
        colFecha: 2,
        colHora: 4,
        colCod: 17,
        colDespacho: 16,
        colHoraDespacho: 18,
      },
      "FEBRERO 2026": {
        disparador: 4,
        colFecha: 2,
        colHora: 3,
        colCod: 18,
        colDespacho: 17,
        colHoraDespacho: 19,
      },
    };

    const ajustes = config[sheetName];
    if (!ajustes) return;

    // LÓGICA PARA REGISTRO DE HORA DE DESPACHO
    if (ajustes.colDespacho && col === ajustes.colDespacho) {
      const valorEstado = e.value.toString().toUpperCase();

      // Si el estado es "D" (Despachado)
      if (valorEstado === "D") {
        const horaActual = Utilities.formatDate(
          new Date(),
          Session.getScriptTimeZone(),
          "HH:mm",
        );
        const celdaHoraS = sheet.getRange(row, ajustes.colHoraDespacho);

        // Solo registra la hora si la celda está vacía     (evita sobrescribir si cambian de D a D)
        if (celdaHoraS.getValue() === "") {
          celdaHoraS.setValue(horaActual);
          celdaHoraS.setNumberFormat("HH:mm");
        }
        return; // Finaliza para evitar ejecutar las otras    lógicas
      }
    }

    // LÓGICA PARA LIMPIAR CÓDIGO (Ya existente)
    if (col === ajustes.colCod) {
      var valorEntrante = e.value.toString();
      if (valorEntrante.indexOf("-") !== -1) {
        var codigoPuro = Number(valorEntrante.split("-")[0].trim());
        if (!isNaN(codigoPuro)) {
          range.setValue(codigoPuro);
          return;
        }
      }
    }

    // LÓGICA DE REGISTRO DE FECHA Y HORA DE CREACIÓN (Solo     si es la columna disparadora)
    if (col !== ajustes.disparador) return;

    const now = new Date();
    const fechaPura = new Date(
      now.getFullYear(),
      now.getMonth(),
      now.getDate(),
    );
    const horaPura = Utilities.formatDate(
      now,
      Session.getScriptTimeZone(),
      "HH:mm",
    );

    const colInicio = Math.min(ajustes.colFecha, ajustes.colHora);
    const ancho = Math.max(ajustes.colFecha, ajustes.colHora) - colInicio + 1;
    const rangoBloque = sheet.getRange(row, colInicio, 1, ancho);

    const valores = rangoBloque.getValues()[0];
    const formatos = rangoBloque.getNumberFormats()[0];

    const idxFecha = ajustes.colFecha - colInicio;
    const idxHora = ajustes.colHora - colInicio;

    let huboCambio = false;

    if (valores[idxFecha] === "") {
      valores[idxFecha] = fechaPura;
      formatos[idxFecha] = "dd/mm/yyyy";
      huboCambio = true;
    }
    if (valores[idxHora] === "") {
      valores[idxHora] = horaPura;
      formatos[idxHora] = "HH:mm";
      huboCambio = true;
    }

    if (huboCambio) {
      rangoBloque.setValues([valores]);
      rangoBloque.setNumberFormats([formatos]);
    }
  }
  ```

## 2. Reporte de Clientes Nuevos

> ORDEN DE PEDIDOS

- Ir a AppScript

- Crear la hoja de funciones Reporte_Mes:

  ```js
  // ==========================================
  // MENÚ PERSONALIZADO PARA CIERRE DE MES
  // ==========================================

  function onOpen() {
    const ui = SpreadsheetApp.getUi();
    ui.createMenu("CIERRE DE MES")
      .addItem("Clientes Nuevos", "generarReporteNuevos")
      .addItem("Limpiar Reporte", "limpiarReporte")
      .addToUi();
  }

  function generarReporteNuevos() {
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    const hojaActiva = ss.getActiveSheet();
    const nombreHojaActiva = hojaActiva.getName();
    const ui = SpreadsheetApp.getUi();

    // Verificar que se ejecute desde la hoja de pedidos del mes
    if (
      nombreHojaActiva === "SOPORTE_DATOS" ||
      nombreHojaActiva === "REPORTE NUEVOS"
    ) {
      ui.alert(
        "Por favor, ve a tu hoja de trabajo del mes (ej. FEBRERO 2026) y vuelve a presionar el botón.",
      );
      return;
    }

    // Traer los clientes ya registrados desde SOPORTE_DATOS (Columna A)
    const hojaSoporte = ss.getSheetByName("SOPORTE_DATOS");
    if (!hojaSoporte) {
      ui.alert("Error: No se encontró la hoja SOPORTE_DATOS.");
      return;
    }
    const datosRegistrados = hojaSoporte
      .getRange("A2:A")
      .getValues()
      .flat()
      .filter(String);
    // Creacion de  una lista rápida en mayúsculas para comparar sin errores
    const setRegistrados = new Set(
      datosRegistrados.map((c) => c.toString().trim().toUpperCase()),
    );

    // Traer los clientes de la hoja del mes actual (Columna D empieza en fila 3)
    const ultFila = hojaActiva.getLastRow();
    if (ultFila < 3) {
      ui.alert("No hay suficientes datos en esta hoja.");
      return;
    }
    const datosMes = hojaActiva
      .getRange(3, 4, ultFila - 2, 1)
      .getValues()
      .flat()
      .filter(String);

    // Contar los clientes que NO están en la base
    const conteoNuevos = {};
    datosMes.forEach((cliente) => {
      const cLimpio = cliente.toString().trim().toUpperCase();
      if (!setRegistrados.has(cLimpio)) {
        // Si no está registrado, se suma al contador
        conteoNuevos[cliente] = (conteoNuevos[cliente] || 0) + 1;
      }
    });

    // Convertir a tabla y ordenar de mayor a menor cantidad de pedidos
    const arrayResultados = Object.keys(conteoNuevos).map((c) => [
      c,
      conteoNuevos[c],
    ]);
    arrayResultados.sort((a, b) => b[1] - a[1]); // Orden descendente

    // Preparar la hoja de REPORTE NUEVOS
    let hojaReporte = ss.getSheetByName("REPORTE NUEVOS");
    if (!hojaReporte) {
      hojaReporte = ss.insertSheet("REPORTE NUEVOS"); // Si no existe, la crea
    }
    hojaReporte.clear(); // Limpiar datos anteriores

    // Títulos
    hojaReporte
      .getRange("A1:B1")
      .setValues([
        ["CLIENTE TEMPORAL (No Registrado en BASE)", "CANTIDAD DE PEDIDOS"],
      ])
      .setFontWeight("bold")
      .setBackground("#9fc5e8")
      .setFontColor("black")
      .setHorizontalAlignment("center")
      .setVerticalAlignment("middle"); // <-- En Apps Script se usa "middle"

    hojaReporte.setRowHeight(1, 40);
    hojaReporte.setColumnWidth(1, 370);
    hojaReporte.setColumnWidth(2, 190);

    if (arrayResultados.length > 0) {
      const rangoDestino = hojaReporte.getRange(
        2,
        1,
        arrayResultados.length,
        2,
      );
      rangoDestino.setValues(arrayResultados);

      // 7. Colorear de verde los que tienen MÁS de 5 pedidos
      const coloresFondo = arrayResultados.map((fila) => {
        if (fila[1] > 3) {
          return ["#31b900", "#31b900"]; // Verde
        } else {
          return ["#ffffff", "#ffffff"]; // Blanco
        }
      });
      rangoDestino.setBackgrounds(coloresFondo);
    } else {
      hojaReporte
        .getRange("A2")
        .setValue("¡Todos los clientes de este mes ya están registrados!");
    }

    // Ajustes visuales
    ss.setActiveSheet(hojaReporte); // Lleva automáticamente a ver el reporte
  }

  function limpiarReporte() {
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    const hojaReporte = ss.getSheetByName("REPORTE NUEVOS");
    if (hojaReporte) {
      hojaReporte.clear();
      hojaReporte.getRange("A1").setValue("Esperando nuevo análisis...");
      SpreadsheetApp.getUi().alert(
        "Reporte limpiado y listo para el próximo mes.",
      );
    } else {
      SpreadsheetApp.getUi().alert(
        "No existe la hoja de reporte para limpiar.",
      );
    }
  }
  ```

## 3. Depurar Clientes Preliminar

> ORDEN DE PEDIDOS

- Abrir ultimas tres hojas (ENERO 2026, DICIEMBRE 25, NOVIEMEBRE 25)

- Ajustar hojas para que conicida con la funcionalidad actual
  - Aplicar filtro en los encabezados
  - Filtrar por datos no validados
  - Hacer respectivo ajuste

- Abrir Apps Script

- En la hoja de Menu_Funciones:
  ```js
    function onOpen() {
      const ui = SpreadsheetApp.getUi();
      ui.createMenu("CIERRE DE MES")
        .addItem("📑 Clientes Nuevos", "generarReporteNuevos")
        .addItem("⚠️ Limpiar Reporte", "limpiarReporte")
        .addSeparator() // Añade una línea divisoria en el menú
        .addItem("📑 Clientes a Depurar", "depurarClientes")
        .addToUi();
    }
  ```

- Crear hoja Depuracion_Preliminar:
  ```js
  // ==========================================
  // FUNCIÓN DEPURAR BASE DE DATOS
  // ==========================================

  function depurarClientes() {
    const ui = SpreadsheetApp.getUi();
    const ss = SpreadsheetApp.getActiveSpreadsheet();

    // 1. Pedirle al usuario los meses a evaluar
    const respuesta = ui.prompt(
      "Depuración de Clientes",
      "Escribe el nombre exacto de las 3 hojas a evaluar,   separadas por coma.\nEjemplo: NOVIEMBRE 25, DICIEMBRE   25, ENERO 2026",
      ui.ButtonSet.OK_CANCEL,
    );

    if (respuesta.getSelectedButton() !== ui.Button.OK) return;

    // Limpiar y separar los nombres ingresados
    const hojasText = respuesta.getResponseText();
    const nombresHojas = hojasText.split(",").map((nombre) => nombre.trim());

    if (nombresHojas.length === 0) {
      ui.alert("Debes ingresar al menos el nombre de una hoja.  ");
      return;
    }

    ui.alert(
      "⏳ Procesando más de 3000 registros en  memoria... Esto tomará solo unos segundos. Dale a Aceptar. ",
    );

    // 2. Obtener la lista completa de clientes de la BASE  (usamos SOPORTE_DATOS)
    const hojaSoporte = ss.getSheetByName("SOPORTE_DATOS");
    if (!hojaSoporte) {
      ui.alert("Error: No se encontró la hoja SOPORTE_DATOS.  ");
      return;
    }
    const clientesBase = hojaSoporte
      .getRange("A2:A")
      .getValues()
      .flat()
      .filter(String);

    // 3. Crear un Diccionario para contar rapidísimo
    const diccionario = {};
    clientesBase.forEach((cliente) => {
      if (cliente) {
        // Solo si la celda no está vacía
        const nombreTexto = cliente.toString().trim();
        diccionario[nombreTexto.toUpperCase()] = {
          nombreOriginal: nombreTexto,
          conteos: new Array(nombresHojas.length).fill(0),
        };
      }
    });

    // 4. Leer las hojas de los meses especificados y contar
    const hojasNoEncontradas = [];
    nombresHojas.forEach((nombreMes, indexMes) => {
      const hojaMes = ss.getSheetByName(nombreMes);
      if (!hojaMes) {
        hojasNoEncontradas.push(nombreMes);
        return;
      }

      const ultFila = Math.max(hojaMes.getLastRow(), 3);
      const datosMes = hojaMes
        .getRange(3, 4, ultFila - 2, 1)
        .getValues()
        .flat()
        .filter(String);

      datosMes.forEach((cliente) => {
        // Convertimos a texto antes de usar toUpperCase()  para evitar el error
        const cLimpio = cliente.toString().trim().toUpperCase();
        if (diccionario[cLimpio]) {
          diccionario[cLimpio].conteos[indexMes]++;
        }
      });
    });

    if (hojasNoEncontradas.length > 0) {
      ui.alert(
        "Cuidado: No se encontraron estas hojas: " +
          hojasNoEncontradas.join(", "),
      );
    }

    // 5. Preparar la "Matriz" de Resultados y Colores para   inyectar de un solo golpe
    const matrizDatos = [];
    const matrizColores = [];

    Object.values(diccionario).forEach((obj) => {
      const filaDatos = [obj.nombreOriginal];
      const filaColores = ["#ffffff"]; // Color del nombre  del cliente
      let totalApariciones = 0;

      // Procesar cada mes
      obj.conteos.forEach((conteo) => {
        filaDatos.push(conteo);
        totalApariciones += conteo;

        // Regla: Si aparece menos de 3 veces en el mes,  pintar rojo claro
        if (conteo < 3) {
          filaColores.push("#f4cccc");
        } else {
          filaColores.push("#ffffff");
        }
      });

      filaDatos.push(totalApariciones);
      filaColores.push("#ffffff"); // Color del total

      // Lógica de DECISIÓN (Puedes modificar esto si lo  deseas)
      if (totalApariciones === 0) {
        filaDatos.push("ELIMINAR");
        filaColores.push("#ea9999"); // Rojo fuerte
      } else {
        filaDatos.push("CONSERVAR");
        filaColores.push("#d9ead3"); // Verde claro
      }

      matrizDatos.push(filaDatos);
      matrizColores.push(filaColores);
    });

    // 6. Construir la hoja de DEPURACIÓN CLIENTES
    let hojaDepuracion = ss.getSheetByName("DEPURACIÓN  CLIENTES");
    if (!hojaDepuracion) {
      hojaDepuracion = ss.insertSheet("DEPURACIÓN CLIENTES");
    }
    hojaDepuracion.clear();

    // Crear Títulos dinámicos basados en los meses ingresados
    const titulos = ["CLIENTE BASE", ...nombresHojas, "TOTAL", "DECISIÓN"];
    hojaDepuracion
      .getRange(1, 1, 1, titulos.length)
      .setValues([titulos])
      .setFontWeight("bold")
      .setBackground("#11304c") // Azul oscuro
      .setFontColor("white");

    // Inyectar Datos y Colores masivamente (¡Súper rápido!)
    if (matrizDatos.length > 0) {
      const rangoDestino = hojaDepuracion.getRange(
        2,
        1,
        matrizDatos.length,
        titulos.length,
      );
      rangoDestino.setValues(matrizDatos);
      rangoDestino.setBackgrounds(matrizColores);
    }

    // Ajustes visuales
    hojaDepuracion.autoResizeColumns(1, titulos.length);
    ss.setActiveSheet(hojaDepuracion);
    ui.alert("Análisis de Depuración completado con éxito.");
  }
  ```

## 4. Depurar Clientes Definitivo

- Ir a Apps Script

- Crear hoja Depuracion_Definitiva e insertar:

  ```js
  // ==========================================
  // FUNCIÓN PARA ELIMINAR CLIENTES DE LA BASE ORIGINAL
  // ==========================================

  function ejecutarEliminacionFinal() {
    const ui = SpreadsheetApp.getUi();
    const ss = SpreadsheetApp.getActiveSpreadsheet();
    const hojaDepuracion = ss.getSheetByName("DEPURACIÓN  CLIENTES");

    if (!hojaDepuracion) {
      ui.alert("Error: Primero debes generar el reporte de  depuración.");
      return;
    }

    // 1. Confirmación de seguridad
    const confirmacion = ui.alert(
      "ADVERTENCIA DE ELIMINACIÓN",
      "Estás a punto de borrar permanentemente los clientes   marcados como 'ELIMINAR' en el archivo BASE DE DATOS. \n\n¿Estás seguro de que deseas continuar?",
      ui.ButtonSet.YES_NO,
    );

    if (confirmacion !== ui.Button.YES) return;

    // 2. Obtener la lista de nombres a eliminar desde la   hoja de Depuración
    const datosDepuracion = hojaDepuracion.getDataRange().getValues();
    const nombresAEliminar = new Set();

    // Recorremos la tabla (empezando en fila 2) buscando la  palabra "ELIMINAR" en la última columna
    for (let i = 1; i < datosDepuracion.length; i++) {
      const decision = datosDepuracion[i][datosDepuracion[i].length - 1];
      if (decision === "ELIMINAR") {
        nombresAEliminar.add(
          datosDepuracion[i][0].toString().toUpperCase().trim(),
        );
      }
    }

    if (nombresAEliminar.size === 0) {
      ui.alert("No hay clientes marcados para eliminar.");
      return;
    }

    // 3. Conectar con el archivo BASE DE DATOS
    // Usaremos la URL o ID de tu archivo de base de datos
    const urlBase =
      "https://docs.google.com/spreadsheets/d/  1Cbf5qtZKZvk27Y2qgLyDn5YCVScs3FZ8Wiqq1cc0QCM/edit?  gid=1297009847#gid=1297009847";
    try {
      const ssExterna = SpreadsheetApp.openByUrl(urlBase);
      const hojaBaseOriginal = ssExterna.getSheetByName("BASE");

      if (!hojaBaseOriginal) {
        ui.alert(
          "Error: No se encontró la pestaña 'BASE' en  el archivo externo.",
        );
        return;
      }

      const datosBase = hojaBaseOriginal.getDataRange().getValues();
      let filasBorradas = 0;

      // 4. ELIMINACIÓN INVERSA (De abajo hacia arriba para   no perder el índice de las filas)
      for (let j = datosBase.length - 1; j >= 0; j--) {
        // El nombre del cliente está en la Columna B (índice   1) de tu hoja BASE
        const nombreEnBase = datosBase[j][1].toString().toUpperCase().trim();

        if (nombresAEliminar.has(nombreEnBase)) {
          hojaBaseOriginal.deleteRow(j + 1);
          filasBorradas++;
        }
      }

      ui.alert(
        "¡Operación Exitosa!\nSe han eliminado " +
          filasBorradas +
          " clientes de la Base de Datos.",
      );
    } catch (e) {
      ui.alert(
        "Error de conexión: Verifica que tienes  permisos en el archivo de Base de Datos y que la URL es  correcta.",
      );
    }
  }
  ```

- Ir a hoja de Menu Funciones y Agregar:
    ```js
    function onOpen() {
      const ui = SpreadsheetApp.getUi();
      ui.createMenu("CIERRE DE MES")
        .addItem("📑 Clientes Nuevos", "generarReporteNuevos")
        .addItem("⚠️ Limpiar Reporte", "limpiarReporte")
        .addSeparator() // Añade una línea divisoria en el menú
        .addItem("📑 Clientes a Depurar", "depurarClientes")
        .addItem("⚠️ Depurar Clientes", "ejecutarEliminacionFinal")
        .addToUi();
    }
  ```
