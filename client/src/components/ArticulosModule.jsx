// Archivo: src/components/ArticulosModule.jsx
import React, { useState } from 'react';
import * as XLSX from 'xlsx';

export default function ArticulosModule({ onBack }) {
  const [file, setFile] = useState(null);
  const [tipoEstructura, setTipoEstructura] = useState('simple'); 
  const [tipoOperacion, setTipoOperacion] = useState('crear');     
  const [loading, setLoading] = useState(false);
  const [progresoTexto, setProgresoTexto] = useState('');
  const [porcentajeProgreso, setPorcentajeProgreso] = useState(0);
  const [resultado, setResultado] = useState(null);

  const handleFileSelect = (e) => {
    const selectedFile = e.target.files[0];
    if (selectedFile) {
      setFile(selectedFile);
      setResultado(null);
    }
  };

  const handleProcessUpload = async (e) => {
    e.preventDefault();
    if (!file) {
      alert('Por favor, selecciona un archivo Excel primero.');
      return;
    }

    setLoading(true);
    setResultado(null);
    setProgresoTexto('Leyendo archivo Excel...');
    setPorcentajeProgreso(0);

    const reader = new FileReader();

    reader.onerror = () => {
      setLoading(false);
      alert('Error al leer el archivo en el navegador.');
    };

    reader.onload = async (evt) => {
      try {
        const buffer = evt.target.result;
        const wb = XLSX.read(new Uint8Array(buffer), { type: 'array' });

        // 1. CORRECCIÓN: Apuntar primero a la pestaña exacta del Excel
        const ws = wb.Sheets["LISTA MAKITA BRASIL"] || wb.Sheets["Articulos"] || wb.Sheets[wb.SheetNames[0]];

        const rawData = XLSX.utils.sheet_to_json(ws);
        let payloadFinal = [];

        // ====================================================================
        // LÓGICA 1: ACTUALIZACIÓN EXCLUSIVA DE PRECIOS
        // ====================================================================
        if (tipoOperacion === 'actualizarPrecioBrasil') {
          payloadFinal = rawData.map(row => {
            // 2. CORRECCIÓN: Búsqueda estricta de "PrecioBrasil" blindada contra espacios
            const keyItemCode = Object.keys(row).find(k => k.trim().toLowerCase() === 'itemcode');
            const keyPrecio = Object.keys(row).find(k => k.trim().toLowerCase() === 'preciobrasil');

            const itemCodeRaw = keyItemCode ? row[keyItemCode] : '';
            const precioRaw = keyPrecio ? row[keyPrecio] : '0';

            // Extrae únicamente el valor numérico, eliminando "USD "
            const precioLimpio = String(precioRaw).replace(/[^\d.-]/g, '');
            const precioFinal = parseFloat(precioLimpio) || 0;

            return {
              ItemCode: String(itemCodeRaw).trim(),
              PriceListId: 16,
              Price: precioFinal,
              Currency: "USD" 
            };
          }).filter(item => item.ItemCode !== '' && item.Price > 0);

        // ====================================================================
        // LÓGICA 2: MAESTRO COMPLETO (SIMPLES O COMBOS)
        // ====================================================================
        //} else {
// ... mantén el resto de tu código intacto a partir de aquí


    /*
    reader.onload = async (evt) => {
      try {
        const buffer = evt.target.result;
        const wb = XLSX.read(new Uint8Array(buffer), { type: 'array' });

        const nombreHoja = "Articulos";
        const ws = wb.Sheets[nombreHoja] || wb.Sheets[wb.SheetNames[0]];

        if (!wb.Sheets[nombreHoja]) {
          console.warn(`⚠️ No se encontró la hoja "${nombreHoja}". Se usó la primera pestaña.`);
        }

        const rawData = XLSX.utils.sheet_to_json(ws);
        let payloadFinal = [];

        // ====================================================================
        // LÓGICA 1: ACTUALIZACIÓN EXCLUSIVA DE PRECIOS (NUEVO)
        // ====================================================================
        if (tipoOperacion === 'actualizarPrecioBrasil') {
          payloadFinal = rawData.map(row => {
            const keyItemCode = Object.keys(row).find(k => k.trim().toLowerCase() === 'itemcode');
            const keyPrecio = Object.keys(row).find(k => {
               const lowerK = k.trim().toLowerCase();
               return lowerK === 'preciobrasil' || lowerK === 'lista makita brasil';
            });

            const itemCodeRaw = keyItemCode ? row[keyItemCode] : '';
            const precioRaw = keyPrecio ? row[keyPrecio] : '0';

            const precioLimpio = String(precioRaw || '0').replace(/[^\d.-]/g, '');
            const precioFinal = parseFloat(precioLimpio) || 0;

            return {
              ItemCode: String(itemCodeRaw).trim(),
              PriceListId: 16,
              Price: precioFinal,
              Currency: "USD" 
            };
          }).filter(item => item.ItemCode !== '' && item.Price > 0);
          */
        // ====================================================================
        // LÓGICA 2: MAESTRO COMPLETO (SIMPLES O COMBOS) - ORIGINAL
        // ====================================================================
        } else {
          const articulosMap = {};

          rawData.forEach((row) => {
            const itemCode = row.ItemCode;
            if (!itemCode || String(itemCode).toUpperCase() === 'ITEMCODE') return;

            if (!articulosMap[itemCode]) {
              const preciosList = [];

              Object.keys(row).forEach((colName) => {
                if (colName.toUpperCase().includes('LISTA') || colName.toUpperCase().includes('PRECIO')) {
                  const partes = colName.split('_');
                  const idLista = Number(partes[partes.length - 1]);

                  if (!isNaN(idLista) && row[colName] !== undefined && row[colName] !== '') {
                    preciosList.push({
                      priceListId: idLista,
                      price: Number(row[colName])
                    });
                  }
                }
              });

              articulosMap[itemCode] = {
                itemCode: String(itemCode),
                itemName: row.ItemName ? String(row.ItemName) : '',
                itemType: row.ItemType ? String(row.ItemType) : 'I',
                itemsGroupCode: row.ItemsGroupCode ? Number(row.ItemsGroupCode) : 0,
                treeType: row.TipoLMat ? String(row.TipoLMat) : 'iSales',
                u_EXX_TIPOEXIS: row.U_EXX_TIPOEXIS ? String(row.U_EXX_TIPOEXIS) : '',
                u_EXX_TIPOUMED: row.U_EXX_TIPOUMED ? String(row.U_EXX_TIPOUMED) : '',
                u_EXM_PERCOM: row.U_EXM_PERCOM ? String(row.U_EXM_PERCOM) : '',
                u_EXM_ESTOBS: row.U_EXM_ESTOBS ? String(row.U_EXM_ESTOBS) : '',
                u_MKA_TINCOS: row.U_MKA_TINCOS ? String(row.U_MKA_TINCOS) : '',
                esCombo: tipoEstructura === 'con_bom' || String(row.Tipo || '').toUpperCase() === 'COMBO',
                precios: preciosList,
                componentes: []
              };
            }

            if (tipoEstructura === 'con_bom' && row.Componente_Code) {
              articulosMap[itemCode].componentes.push({
                itemCode: String(row.Componente_Code),
                quantity: Number(row.Componente_Qty || 1)
              });
            }
          });

          payloadFinal = Object.values(articulosMap);
        }

        const totalRegistros = payloadFinal.length;

        if (totalRegistros === 0) {
          alert('No se encontraron registros válidos para procesar o los precios detectados son cero.');
          setLoading(false);
          return;
        }

        // ====================================================================
        // SELECCIÓN DINÁMICA DE ENDPOINTS Y MÉTODOS HTTP
        // ====================================================================
        const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5243';
        let endpointDestino = '';
        let metodoHttp = 'POST'; // Por defecto los maestros usan POST en tu backend

        if (tipoOperacion === 'actualizarPrecioBrasil') {
          endpointDestino = `${baseUrl}/api/Articles/update-price`;
          metodoHttp = 'PATCH'; // CRÍTICO: El endpoint de precios exige PATCH
        } else if (tipoEstructura === 'simple') {
          endpointDestino = tipoOperacion === 'crear' 
            ? `${baseUrl}/api/Articles/crear-masivo-simples` 
            : `${baseUrl}/api/Articles/actualizar-masivo-simples`;
        } else {
          endpointDestino = tipoOperacion === 'crear' 
            ? `${baseUrl}/api/Articles/crear-masivo-combos` 
            : `${baseUrl}/api/Articles/actualizar-masivo-combos`;
        }

        // ESTRATEGIA DE LOTES (BLOQUES DE 50)
        const tamanoBloque = 50;
        let detallesAcumulados = [];

        for (let i = 0; i < totalRegistros; i += tamanoBloque) {
          const bloque = payloadFinal.slice(i, i + tamanoBloque);
          const nroBloqueActual = Math.floor(i / tamanoBloque) + 1;
          const totalBloques = Math.ceil(totalRegistros / tamanoBloque);

          setProgresoTexto(`Procesando bloque ${nroBloqueActual} de ${totalBloques} (${i + 1} al ${Math.min(i + tamanoBloque, totalRegistros)} de ${totalRegistros} artículos)...`);
          setPorcentajeProgreso(Math.round(((i + bloque.length) / totalRegistros) * 100));

          try {
            const response = await fetch(endpointDestino, {
              method: metodoHttp, // Se inyecta PATCH o POST dinámicamente
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(bloque)
            });

            const data = await response.json().catch(() => ({}));

            if (response.ok) {
              const resultadosBloque = data.detalles || data;
              if (Array.isArray(resultadosBloque)) {
                detallesAcumulados.push(...resultadosBloque);
              } else {
                detallesAcumulados.push(data);
              }
            } else {
              detallesAcumulados.push({
                status: 'ERROR_BLOQUE',
                message: `Error HTTP en bloque ${nroBloqueActual}: ${data.message || 'Desconocido'}`
              });
            }
          } catch (bloqueError) {
            detallesAcumulados.push({
              status: 'ERROR_RED',
              message: `Falla de red en bloque ${nroBloqueActual}: ${bloqueError.message}`
            });
          }
        }

        setLoading(false);
        setResultado({
          success: true,
          message: `Proceso finalizado exitosamente.`,
          total: totalRegistros,
          detalles: detallesAcumulados
        });

      } catch (error) {
        setLoading(false);
        alert(`Error general al procesar el archivo: ${error.message}`);
      }
    };

    reader.readAsArrayBuffer(file);
  };

  return (
    <div style={styles.container}>
      <div style={styles.headerRow}>
        <button onClick={onBack} style={styles.backBtn}>
          &larr; Volver al Menú Principal
        </button>
        <h2 style={styles.title}>📦 Módulo de Artículos - Carga Masiva por Lotes</h2>
      </div>

      <p style={styles.subtitle}>
        Sincronización masiva optimizada por bloques hacia la base de datos de SAP Business One.
      </p>

      {/* PANEL DE CONFIGURACIÓN DUAL */}
      <div style={styles.configGrid}>
        <div style={styles.optionsCard}>
          <label style={styles.optionLabel}>1. Seleccione la estructura:</label>
          <div style={styles.radioGroup}>
            <label style={styles.radioLabel}>
              <input 
                type="radio" 
                name="tipoEstructura" 
                checked={tipoEstructura === 'simple'} 
                onChange={() => setTipoEstructura('simple')} 
                disabled={tipoOperacion === 'actualizarPrecioBrasil'}
              />
              📦 Artículos / Producto Simple
            </label>
            <label style={styles.radioLabel}>
              <input 
                type="radio" 
                name="tipoEstructura" 
                checked={tipoEstructura === 'con_bom'} 
                onChange={() => setTipoEstructura('con_bom')} 
                disabled={tipoOperacion === 'actualizarPrecioBrasil'}
              />
              🔗 Artículos / Combos (Lista Materiales)
            </label>
          </div>
        </div>

        <div style={styles.optionsCard}>
          <label style={styles.optionLabel}>2. Seleccione la operación:</label>
          <div style={styles.radioGroup}>
            <label style={styles.radioLabel}>
              <input 
                type="radio" 
                name="tipoOperacion" 
                checked={tipoOperacion === 'crear'} 
                onChange={() => setTipoOperacion('crear')} 
              />
              ➕ Crear Nuevos Registros
            </label>
            <label style={styles.radioLabel}>
              <input 
                type="radio" 
                name="tipoOperacion" 
                checked={tipoOperacion === 'actualizar'} 
                onChange={() => setTipoOperacion('actualizar')} 
              />
              🔄 Actualizar Registros Existentes
            </label>
            {/* AQUÍ ESTÁ EL TERCER BOTÓN AÑADIDO */}
            <label style={styles.radioLabel}>
              <input 
                type="radio" 
                name="tipoOperacion" 
                checked={tipoOperacion === 'actualizarPrecioBrasil'} 
                onChange={() => {
                  setTipoOperacion('actualizarPrecioBrasil');
                  setTipoEstructura('simple'); // Fuerza a 'simple' al seleccionar precios
                }} 
              />
              💲 Actualizar Precio (Lista Brasil - 16)
            </label>
          </div>
        </div>
      </div>

      <form onSubmit={handleProcessUpload} style={styles.uploadCard}>
        <div style={styles.dropZone}>
          <span style={styles.uploadIcon}>📁</span>
          <h3>Seleccione el archivo fuente (.xlsx)</h3>
          <p style={styles.fileInfo}>
            {file ? `Archivo seleccionado: <strong>${file.name}</strong>` : 'Ningún archivo seleccionado'}
          </p>
          
          <label style={styles.fileButton}>
            Examinar equipo (Open Dialog)
            <input 
              type="file" 
              accept=".xlsx, .xls" 
              onChange={handleFileSelect} 
              style={{ display: 'none' }} 
            />
          </label>
        </div>

        {file && !loading && (
          <button type="submit" style={styles.submitBtn}>
            Ejecutar {tipoOperacion === 'actualizarPrecioBrasil' ? 'ACTUALIZACIÓN DE PRECIOS' : `${tipoOperacion.toUpperCase()} Masiva`} en Lotes
          </button>
        )}

        {loading && (
          <div style={styles.progressContainer}>
            <p style={styles.progressText}>⏳ {progresoTexto}</p>
            <div style={styles.progressBarWrapper}>
              <div style={{ ...styles.progressBarFill, width: `${porcentajeProgreso}%` }}></div>
            </div>
            <p style={styles.progressPercentage}>{porcentajeProgreso}% completado</p>
          </div>
        )}
      </form>

      {resultado && (
        <div style={resultado.success ? styles.resultBox : styles.errorBox}>
          <h4 style={{ margin: '0 0 10px 0', color: resultado.success ? '#2e7d32' : '#c62828' }}>
            {resultado.success ? '✅ ¡Operación Exitosa!' : '❌ Error en la Operación'}
          </h4>
          <p style={{ margin: '0 0 10px 0', color: '#333' }}>
            {resultado.message} Se procesaron <strong>{resultado.total}</strong> artículos.
          </p>
          
          <div style={styles.codeBlockWithScroll}>
            <pre style={{ margin: 0, fontFamily: 'monospace' }}>
              {JSON.stringify(resultado.detalles, null, 2)}
            </pre>
          </div>
        </div>
      )}
    </div>
  );
}

const styles = {
  container: { background: '#ffffff', padding: '30px', borderRadius: '10px', boxShadow: '0 4px 12px rgba(0,0,0,0.06)' },
  headerRow: { display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '15px', flexWrap: 'wrap', gap: '10px' },
  backBtn: { backgroundColor: '#f0f0f0', color: '#333', border: 'none', padding: '8px 16px', borderRadius: '4px', cursor: 'pointer', fontWeight: '600' },
  title: { margin: 0, color: '#212121', fontSize: '20px' },
  subtitle: { color: '#666', marginBottom: '25px', fontSize: '14px' },
  configGrid: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '15px', marginBottom: '20px' },
  optionsCard: { backgroundColor: '#f9f9f9', padding: '15px 20px', borderRadius: '8px', border: '1px solid #e0e0e0' },
  optionLabel: { display: 'block', fontWeight: '600', marginBottom: '10px', color: '#333', fontSize: '14px' },
  radioGroup: { display: 'flex', flexDirection: 'column', gap: '8px' },
  radioLabel: { display: 'flex', alignItems: 'center', gap: '8px', fontSize: '14px', color: '#444', cursor: 'pointer' },
  uploadCard: { border: '2px dashed #d32f2f', padding: '30px', borderRadius: '8px', textAlign: 'center', backgroundColor: '#fffcfc' },
  dropZone: { display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '10px' },
  uploadIcon: { fontSize: '40px' },
  fileInfo: { color: '#555', fontSize: '13px', margin: '5px 0 15px 0' },
  fileButton: { backgroundColor: '#212121', color: '#fff', padding: '10px 20px', borderRadius: '6px', cursor: 'pointer', fontWeight: '600', fontSize: '14px', display: 'inline-block', transition: 'background 0.2s' },
  submitBtn: { marginTop: '20px', width: '100%', padding: '14px', backgroundColor: '#d32f2f', color: '#fff', border: 'none', borderRadius: '6px', fontSize: '16px', fontWeight: 'bold', cursor: 'pointer' },
  progressContainer: { marginTop: '20px', textAlign: 'left' },
  progressText: { fontSize: '14px', fontWeight: '600', color: '#333', marginBottom: '8px' },
  progressBarWrapper: { width: '100%', backgroundColor: '#e0e0e0', borderRadius: '6px', height: '14px', overflow: 'hidden' },
  progressBarFill: { backgroundColor: '#d32f2f', height: '100%', transition: 'width 0.3s ease-in-out' },
  progressPercentage: { fontSize: '12px', color: '#666', marginTop: '5px', textAlign: 'right' },
  resultBox: { marginTop: '25px', padding: '20px', backgroundColor: '#e8f5e9', borderRadius: '8px', border: '1px solid #c8e6c9' },
  errorBox: { marginTop: '25px', padding: '20px', backgroundColor: '#ffebee', borderRadius: '8px', border: '1px solid #ffcdd2' },
  codeBlockWithScroll: { 
    backgroundColor: '#fff', 
    padding: '12px', 
    borderRadius: '6px', 
    fontSize: '12px', 
    maxHeight: '300px', 
    overflowY: 'auto', 
    overflowX: 'auto', 
    marginTop: '10px', 
    border: '1px solid #ddd',
    textAlign: 'left'
  }
};