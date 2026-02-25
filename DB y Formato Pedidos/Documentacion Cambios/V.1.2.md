# ***Segunda Implementación de Cambios***

## 1. Acciones Simples
* Eliminar validaciones innecesarias 

* Eliminar hojas inncesarias (SYNC_BASE, HojaOrden)

* Asginar color a COD_VEHICULO
> BASE DE DATOS
* Blanquear celdas 

## 2. Faltantes y Duplicados
> ORDEN P. Y BASE
* Organizar de la A a la Z

* Verificar asosiacion de los datos al organizar

> ORDEN DE PEDIDOS
* Quitar resaltado de AINECOL

* Ajustar formula en columna FALTANTES
    ```excel
    =ARRAYFORMULA(SI(A3:A=""; ""; SI(ESERROR(COINCIDIR(ESPACIOS(A3:A); ESPACIOS(IMPORTRANGE("1nMmcyc9bCW..."; "BASE!B2:B")); 0)); "ESTE FALTA EN BASE"; "EN BASE")))
    ```

* Ajustar formula en colunma DUPLICADO
    ```excel
    =ARRAYFORMULA(SI(A2:A=""; ""; SI(CONTAR.SI(ESPACIOS(A2:A); ESPACIOS(A2:A)) > 1; "DUPLICADO"; "UNICO")))
    ```

> BASE DE DATOS
* Ajustar formula en columna FALTANTES (Recordar dar permisos)
    ```excel
    =ARRAYFORMULA(SI(B2:B=""; ""; SI(ESERROR(COINCIDIR(ESPACIOS(B2:B); ESPACIOS(IMPORTRANGE("1wUoMJg8gMo..."; "BASE CLIENTES!A2:A")); 0)); "ESTE FALTA EN PEDIDOS"; "EN PEDIDOS")))
    ```

* Ajustar formula en columna DUPLICADO
    ```excel
    =ARRAYFORMULA(SI(B2:B=""; ""; SI(CONTAR.SI(ESPACIOS(B2:B); ESPACIOS(B2:B)) > 1; "DUPLICADO"; "UNICO")))
    ```

> ORDEN DE PEDIDOS y BASE
* Evaluar los resultados de las columnas y hacer los cambios correspondientes

> BASE DE DATOS
* Volver a insertar formula de asignacion de zona
    ```excel
    =ARRAYFORMULA(SI(D2:D=""; ""; BUSCARV(D2:D; 'BARRIO - ZONAS'!$A$2:$B; 2; FALSO)))
    ```

* Insertar ZONA(COMUNA), Hospitales, SENAs en el listado de barrios

* Ajustar registros de zonas donde sea necesario

> ORDEN DE PEDIDOS y BASE
* Suprimir formulas anteriores

## 3. Migrar Columnas y Datos 
> BASE DE DATOS
* Insertar las nuevas columnas
* Crear menu desplegable para producto
* Copiar y pegar datos

## 4. Adaptacacion de la Migracion
> ORDEN DE PEDIDOS
* Eliminar hoja ***BASE CLIENTES***

* Crear hoja ***SOPORTE_DATOS***

* En la celda de la hoja aplicar la formula
    ```excel
    =IMPORTRANGE("1Z1aXxkaG..."; "BASE!B2:B")
    ```
* Aplicar en las hojas necesarias:

* Selecciona el rango de clientes (desde D3 hasta el final de la columna).

* Eliminar validaciones previas

* Ve al menú Insertar > Desplegable (o Datos > Validación de datos).

* En el panel de la derecha, en "Criterio", elige "Menú desplegable (de un intervalo)".

* Haz clic en el icono de la cuadrícula para seleccionar el rango y busca la hoja SOPORTE_DATOS.

* Selecciona toda la columna A (ejemplo: SOPORTE_DATOS!$A$2:$A).

## 5. Modificacion Relfejo de Datos
> ORDEN DE PEDIDOS
* Eliminar las formulas de las columnas reflejo
* Insertar y estirar la siguiente formula:

    HOJA PRINCIPAL
    ```excel
    =SI(D3=""; ""; SI.ERROR(BUSCARV(D3; IMPORTRANGE("1Z1aXxka..."; "BASE!B:F"); 2; FALSO); ""))
    ```


    RUTA SAN PEDRO y RUTAS DOMINGOS
    ```excel
    =SI(A2=""; ""; SI.ERROR(BUSCARV(A2; IMPORTRANGE("1Z1aXxka..."; "BASE!B:F"); 2; FALSO); ""))
    ```


    CAJA
    ```excel
    =SI(E3=""; ""; SI.ERROR(BUSCARV(E3; IMPORTRANGE("1Z1aXxka..."; "BASE!B:F"); 2; FALSO); ""))
    ```

















































































