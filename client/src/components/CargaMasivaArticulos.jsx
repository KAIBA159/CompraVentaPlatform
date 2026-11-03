// Archivo: src/components/CargaMasivaArticulos.jsx
import React, { useState } from 'react';
import * as XLSX from '';
import { articulosService } from '../services/articulosService'; 

export default function CargaMasivaArticulos() {
  const [archivo, setArchivo] = useState(null);
  const [estructura, setEstructura] = useState('simple'); 
  const [operacion, setOperacion] = useState('actualizar'); 
  const [estadoCarga, setEstadoCarga] = useState({ cargando: false, mensaje: '', detalles: null });

  const handleFileChange = (event) => {
    setArchivo(event.target.files[0]);
    setEstadoCarga({ cargando: false, mensaje: '', detalles: null });
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    
    if (!archivo) {
      setEstadoCarga({ cargando: false, mensaje: '❌ Por favor, selecciona un archivo.', detalles: null });
      return;
    }

    setEstadoCarga({ cargando: true, mensaje: 'Procesando archivo Excel y enviando a .NET...', detalles: null });

    const reader = new FileReader();
    
    reader.onload = async (e) => {
      try {
        const data = new Uint8Array(e.target.result);
        const workbook = XLSX.read(data, { type: 'array' });
        const firstSheetName = workbook.SheetNames[0];
        const worksheet = workbook.Sheets[firstSheetName];
        
        const excelData = XLSX.utils.sheet_to_json(worksheet);

        // 1. LÓGICA: ACTUALIZAR FABRICANTE Y PAÍS
        if (operacion === 'actualizar') {
          const payloadBackend = excelData.map(row => ({
            ItemCode: String(row.ItemCode || '').trim(),
            Manufacturer: parseInt(row.FirmCode, 10) || 0, 
            CountryOfOrigin: String(row.ISOriCntry || '').trim()
          }));

          const response = await articulosService.actualizarFabricantePais(payloadBackend);
          setEstadoCarga({ 
            cargando: false, 
            mensaje: `✅ ¡Misión cumplida! 🎉 Se han actualizado exitosamente ${payloadBackend.length} artículos en la base de datos.`, 
            detalles: response 
          });

        
          
          // 2. LÓGICA: ACTUALIZAR LISTA BRASIL
        } else if (operacion === 'actualizarPrecioBrasil') {

          const payloadBackend = excelData.map(row => {
            // 1. Buscamos dinámicamente la llave de ItemCode
            const keyItemCode = Object.keys(row).find(k => k.trim().toLowerCase() === 'itemcode');
            
            // 2. Buscamos dinámicamente la llave del precio para Brasil
            const keyPrecio = Object.keys(row).find(k => {
               const lowerK = k.trim().toLowerCase();
               return lowerK === 'brasil' || lowerK === 'preciobrasil' || lowerK === 'lista makita brasil';
            });

            // 3. Extraemos los valores usando las llaves encontradas
            const itemCodeRaw = keyItemCode ? row[keyItemCode] : '';
            const precioRaw = keyPrecio ? row[keyPrecio] : '0';
            const precioLimpio = String(precioRaw).replace(/[^\d.-]/g, '');
            
            console.log("Fila Cruda:", row, " | ItemCode Leído:", itemCodeRaw, " | Precio Leído:", precioRaw, " | Precio Limpio:", precioLimpio);


            return {
              ItemCode: String(itemCodeRaw).trim(),
              PriceListId: 16,
              Price: parseFloat(precioLimpio) || 0,
              Currency: "USD" 
            };
          }).filter(item => item.ItemCode !== '' && item.Price > 0);

          if (payloadBackend.length === 0) {
            setEstadoCarga({ 
                cargando: false, 
                mensaje: '❌ Error: No se encontraron artículos válidos o los precios detectados son cero. Revise los títulos del Excel (debe decir "Brasil").' 
            });
            return;
          }

          const response = await articulosService.actualizarPrecioLista(payloadBackend);
          setEstadoCarga({ 
            cargando: false, 
            mensaje: `✅ ¡Excelente trabajo! 📦 Acabas de actualizar el precio de ${payloadBackend.length} artículos en la Lista 16.`, 
            detalles: response 
          });




          // --- 3. NUEVA LÓGICA: ACTUALIZAR LISTA BOLIVIA ---
        } else if (operacion === 'actualizarPrecioBolivia') {
          const payloadBackend = excelData.map(row => {
            // Nota: Lee la columna "PrecioBolivia". Si en el Excel la columna se llama diferente, ajusta este nombre.
            const precioLimpio = String(row.PrecioBolivia || '0').replace(/[^\d.-]/g, '');
            return {
              ItemCode: String(row.ItemCode || '').trim(),
              PriceListId: 8, // ID estricto para la lista de Bolivia en SAP
              Price: parseFloat(precioLimpio) || 0,
              Currency: "USD" 
            };
          }).filter(item => item.ItemCode !== '' && item.Price > 0);

          if (payloadBackend.length === 0) {
            setEstadoCarga({ 
                cargando: false, 
                mensaje: '❌ Error: No se encontraron artículos válidos o los precios detectados son cero. Revise que la columna en el Excel se llame "PrecioBolivia".' 
            });
            return;
          }

          const response = await articulosService.actualizarPrecioLista(payloadBackend);
          setEstadoCarga({ 
            cargando: false, 
            mensaje: `✅ ¡Excelente trabajo! 📦 Acabas de actualizar el precio de ${payloadBackend.length} artículos en la Lista 8 (Bolivia).`, 
            detalles: response 
          });
        // -------------------------------------------------

        // --- 4. NUEVA LÓGICA: ACTUALIZAR LISTA JAPAN ---
        } else if (operacion === 'actualizarPrecioJapan') {
          const payloadBackend = excelData.map(row => {
            // Busca la columna "JAPAN" según tu Excel
            const precioRaw = row.JAPAN || row.Japan || row.japan || row.PrecioJapan || '0';
            const precioLimpio = String(precioRaw).replace(/[^\d.-]/g, '');
            return {
              ItemCode: String(row.ItemCode || '').trim(),
              PriceListId: 15, // ID estricto para la lista de Japan en SAP
              Price: parseFloat(precioLimpio) || 0,
              Currency: "USD" 
            };
          }).filter(item => item.ItemCode !== '' && item.Price > 0);

          if (payloadBackend.length === 0) {
            setEstadoCarga({ 
                cargando: false, 
                mensaje: '❌ Error: No se encontraron artículos válidos o los precios detectados son cero. Revise que la columna en el Excel se llame "JAPAN".' 
            });
            return;
          }

          const response = await articulosService.actualizarPrecioLista(payloadBackend);
          setEstadoCarga({ 
            cargando: false, 
            mensaje: `✅ ¡Excelente trabajo! 📦 Acabas de actualizar el precio de ${payloadBackend.length} artículos en la Lista 15 (Japan).`, 
            detalles: response 
          });
        // -------------------------------------------------  

          // --- NUEVA LÓGICA: ACTUALIZAR LISTA KUNSHANG ---
        } else if (operacion === 'actualizarPrecioKunshang') {
          const payloadBackend = excelData.map(row => {
            // Se alinea a tu captura de pantalla: busca la columna "KUNSHANG"
            const precioRaw = row.KUNSHANG || row.Kunshang || row.kunshang || row.PrecioKunshang || '0';
            const precioLimpio = String(precioRaw).replace(/[^\d.-]/g, '');
            return {
              ItemCode: String(row.ItemCode || '').trim(),
              PriceListId: 13, // ID estricto para Kunshang en SAP
              Price: parseFloat(precioLimpio) || 0,
              Currency: "USD" 
            };
          }).filter(item => item.ItemCode !== '' && item.Price > 0);

          if (payloadBackend.length === 0) {
            setEstadoCarga({ 
                cargando: false, 
                mensaje: '❌ Error: No se encontraron artículos válidos o los precios detectados son cero. Revise que la columna en el Excel se llame "KUNSHANG".' 
            });
            return;
          }

          const response = await articulosService.actualizarPrecioLista(payloadBackend);
          setEstadoCarga({ 
            cargando: false, 
            mensaje: `✅ ¡Excelente trabajo! 📦 Acabas de actualizar el precio de ${payloadBackend.length} artículos en la Lista 13 (Kunshang).`, 
            detalles: response 
          });
        // -------------------------------------------------

      
      // --- NUEVA LÓGICA: ACTUALIZAR LISTA CHINA ---
        } else if (operacion === 'actualizarPrecioChina') {
          const payloadBackend = excelData.map(row => {
            // Busca la columna "CHINA" según tu captura de Excel
            const precioRaw = row.CHINA || row.China || row.china || row.PrecioChina || '0';
            const precioLimpio = String(precioRaw).replace(/[^\d.-]/g, '');
            return {
              ItemCode: String(row.ItemCode || '').trim(),
              PriceListId: 14, // ID estricto para China en SAP
              Price: parseFloat(precioLimpio) || 0,
              Currency: "USD" 
            };
          }).filter(item => item.ItemCode !== '' && item.Price > 0);

          if (payloadBackend.length === 0) {
            setEstadoCarga({ 
                cargando: false, 
                mensaje: '❌ Error: No se encontraron artículos válidos o los precios detectados son cero. Revise que la columna en el Excel se llame "CHINA".' 
            });
            return;
          }

          const response = await articulosService.actualizarPrecioLista(payloadBackend);
          setEstadoCarga({ 
            cargando: false, 
            mensaje: `✅ ¡Excelente trabajo! 📦 Acabas de actualizar el precio de ${payloadBackend.length} artículos en la Lista 14 (China).`, 
            detalles: response 
          });
        // -------------------------------------------------  





        // 3. LÓGICA: CREAR NUEVOS REGISTROS
        } else if (operacion === 'crear') {
          const articulosDePrueba = [
            { itemCode: 'MKP001', itemName: 'Taladro Percutor Makita 13mm', itemsGroupCode: 101 }
          ];

          const response = await articulosService.crearMasivos(articulosDePrueba);
          setEstadoCarga({ 
            cargando: false, 
            mensaje: `✅ ¡Todo listo! 🚀 ${articulosDePrueba.length} registros fueron creados sin problemas en SAP B1.`, 
            detalles: response.detalles || response 
          });
        }

      } catch (error) {
        console.error('Error parseando/enviando Excel:', error);
        setEstadoCarga({ cargando: false, mensaje: `❌ Error: ${error.message}`, detalles: null });
      }
    };

    reader.readAsArrayBuffer(archivo);
  };

  return (
    <div style={styles.container}>
      <h2>📦 Módulo de Artículos - Carga Masiva por Lotes</h2>
      <p style={styles.subtitle}>Sincronización masiva optimizada por bloques hacia la base de datos de SAP Business One.</p>
      
      <form onSubmit={handleSubmit} style={styles.form}>
        <div style={styles.radioContainer}>
          <div style={styles.radioBox}>
            <p style={styles.radioTitle}>1. Seleccione la estructura:</p>
            <label style={styles.radioLabel}>
              <input type="radio" name="estructura" checked={estructura === 'simple'} onChange={() => setEstructura('simple')} />
              📦 Artículos / Producto Simple
            </label>
            <label style={styles.radioLabel}>
              <input type="radio" name="estructura" checked={estructura === 'combo'} onChange={() => setEstructura('combo')} />
              🔗 Artículos / Combos (Lista Materiales)
            </label>
          </div>

          <div style={styles.radioBox}>
            <p style={styles.radioTitle}>2. Seleccione la operación:</p>
            <label style={styles.radioLabel}>
              <input type="radio" name="operacion" checked={operacion === 'crear'} onChange={() => setOperacion('crear')} />
              ➕ Crear Nuevos Registros
            </label>

            <label style={styles.radioLabel}>
              <input type="radio" name="operacion" checked={operacion === 'actualizar'} onChange={() => setOperacion('actualizar')} />
              🔄 Actualizar Fabricante / País
            </label>

            <label style={styles.radioLabel}>
              <input type="radio" name="operacion" checked={operacion === 'actualizarPrecioBrasil'} onChange={() => setOperacion('actualizarPrecioBrasil')} />
              💲 Actualizar Precio (Lista Brasil - 16)
            </label>

            {/* --- NUEVO RADIO BUTTON PARA BOLIVIA AÑADIDO AQUÍ --- */}
            <label style={styles.radioLabel}>
              <input type="radio" name="operacion" checked={operacion === 'actualizarPrecioBolivia'} onChange={() => setOperacion('actualizarPrecioBolivia')} />
              💲 Actualizar Precio (Lista Bolivia - 8)
            </label>
            {/* -------------------------------------------------- */}

            <label style={styles.radioLabel}>
              <input type="radio" name="operacion" checked={operacion === 'actualizarPrecioJapan'} onChange={() => setOperacion('actualizarPrecioJapan')} />
              💲 Actualizar Precio (Lista Japan - 15)
            </label>

            <label style={styles.radioLabel}>
              <input type="radio" name="operacion" checked={operacion === 'actualizarPrecioKunshang'} onChange={() => setOperacion('actualizarPrecioKunshang')} />
              💲 Actualizar Precio (Lista Kunshang - 13)
            </label>


            <label style={styles.radioLabel}>
              <input type="radio" name="operacion" checked={operacion === 'actualizarPrecioChina'} onChange={() => setOperacion('actualizarPrecioChina')} />
              💲 Actualizar Precio (Lista China - 14)
            </label>


          </div>
        </div>

        <div style={styles.uploadArea}>
          <h3 style={{color: '#ffb300', margin: '0 0 10px 0'}}>📁</h3>
          <h3 style={{margin: '0 0 15px 0', color: '#444'}}>Seleccione el archivo fuente (.)</h3>
          <input type="file" onChange={handleFileChange} accept=".csv, ., .xls" style={styles.fileInput} id="fileInput" />
        </div>

        <button type="submit" style={styles.button} disabled={estadoCarga.cargando || !archivo}>
          {estadoCarga.cargando ? 'Sincronizando con SAP B1...' : 'Iniciar Procesamiento'}
        </button>
      </form>

      {estadoCarga.mensaje && (
        <div style={{ ...styles.mensajeBox, backgroundColor: estadoCarga.mensaje.includes('❌') ? '#f8d7da' : '#d4edda' }}>
          <h3 style={{ margin: '0 0 5px 0', color: estadoCarga.mensaje.includes('❌') ? '#721c24' : '#155724' }}>
            {estadoCarga.mensaje}
          </h3>
          
          {!estadoCarga.mensaje.includes('❌') && (
            <p style={{ margin: 0, color: '#155724', fontSize: '14px' }}>
              Modalidad: <strong>{estructura === 'simple' ? 'Artículos Simples' : 'Combos'}</strong> | 
              Operación: <strong>{operacion.toUpperCase()}</strong>
            </p>
          )}

          {estadoCarga.detalles && (
            <pre style={styles.detallesPre}>{JSON.stringify(estadoCarga.detalles, null, 2)}</pre>
          )}
        </div>
      )}
    </div>
  );
}

const styles = {
  container: { padding: '30px', background: '#fdfdfd', borderRadius: '8px', boxShadow: '0 4px 15px rgba(0,0,0,0.05)', maxWidth: '900px', margin: '0 auto' },
  subtitle: { textAlign: 'center', color: '#666', marginBottom: '30px' },
  form: { display: 'flex', flexDirection: 'column', gap: '20px' },
  radioContainer: { display: 'flex', gap: '20px', justifyContent: 'space-between', flexWrap: 'wrap' },
  radioBox: { flex: 1, minWidth: '300px', padding: '15px 20px', background: '#f9f9f9', border: '1px solid #e0e0e0', borderRadius: '6px' },
  radioTitle: { margin: '0 0 15px 0', fontWeight: 'bold', fontSize: '14px', textAlign: 'center', color: '#333' },
  radioLabel: { display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '10px', fontSize: '14px', cursor: 'pointer', color: '#555' },
  uploadArea: { border: '1px dashed #d32f2f', padding: '40px 20px', textAlign: 'center', borderRadius: '6px', background: '#fffcfc' },
  fileInput: { margin: '0 auto', display: 'block', padding: '10px' },
  button: { padding: '14px 20px', backgroundColor: '#333', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold', fontSize: '15px', marginTop: '10px' },
  mensajeBox: { marginTop: '20px', padding: '15px', borderRadius: '4px', border: '1px solid transparent', color: '#333' },
  detallesPre: { fontSize: '12px', background: 'rgba(255,255,255,0.7)', padding: '10px', overflow: 'auto', marginTop: '10px', borderRadius: '4px' }
};