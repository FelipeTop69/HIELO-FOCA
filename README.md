# HIELO-FOCA

```js
function darAccesoATodasLasHojas() {
  var ss = SpreadsheetApp.getActiveSpreadsheet();
  var hojas = ss.getSheets();
  var miEmail = "xxplaygame02xx@gmail.com"; 

  hojas.forEach(function(hoja) {
    var protecciones = hoja.getProtections(SpreadsheetApp.ProtectionType.SHEET);
    protecciones.forEach(function(p) {
      p.addEditor(miEmail);
    });
    
    var proteccionesRango = hoja.getProtections(SpreadsheetApp.ProtectionType.RANGE);
    proteccionesRango.forEach(function(p) {
      p.addEditor(miEmail);
    });
  });
}
```
