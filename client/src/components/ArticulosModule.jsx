import React, { useState } from 'react';
import * as XLSX from 'xlsx';

export default function ArticulosModule({ onBack }) {
  const [file, setFile] = useState(null);
  const [tipoEstructura, setTipoEstructura] = useState('simple'); // 'simple' | 'con_bom'
  const [tipoOperacion, setTipoOperacion] = useState('crear');     // 'crear' | 'actualizar'
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

        const nombreHoja = "Articulos";
        const ws = wb.Sheets[nombreHoja] || wb.Sheets[wb.SheetNames[0]];

        if (!wb.Sheets[nombreHoja]) {
          console.warn(`⚠️ No se encontró la hoja "${nombreHoja}". Se usó la primera pestaña.`);
        }

        const rawData = XLSX.utils.sheet_to_json(ws);
        const articulosMap = {};

        rawData.forEach((row) => {
          const itemCode = row.ItemCode;
          // FILTRO DE SEGURIDAD: Omite filas vacías o si el código es la cabecera 'ITEMCODE'
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
              itemName: String(row.ItemName || ''),
              itemType: String(row.ItemType || 'I'),
              itemsGroupCode: Number(row.ItemsGroupCode || 100),
              // NUEVOS CAMPOS LOGÍSTICOS PARA COMBOS
              invntItem: String(row.InvntItem || 'N'),
              sellItem: String(row.SellItem || 'Y'),
              prchselitem: String(row.Prchselitem || 'N'),
              treeType: String(row.TipoLMat || 'A'),
              // CAMPOS SUNAT Y ADICIONALES
              u_EXX_TIPOEXIS: String(row.U_EXX_TIPOEXIS || ''),
              u_EXX_TIPOUMED: String(row.U_EXX_TIPOUMED || ''),
              u_EXM_PERCOM: String(row.U_EXM_PERCOM || ''),
              u_EXM_ESTOBS: String(row.U_EXM_ESTOBS || ''),
              u_MKA_TINCOS: String(row.U_MKA_TINCOS || ''),
              esCombo: tipoEstructura === 'con_bom' || String(row.Tipo || '').toUpperCase() === 'COMBO',
              precios: preciosList,
              componentes: []
            };
          }

          // CAPTURA DE COMPONENTES DE LA LISTA DE MATERIALES (BOM)
          if (tipoEstructura === 'con_bom' && row.Componente_Code) {
            articulosMap[itemCode].componentes.push({
              itemCode: String(row.Componente_Code),
              quantity: Number(row.Componente_Qty || 1)
            });
          }
        });

        const payloadFinal = Object.values(articulosMap);
        const totalRegistros = payloadFinal.length;

        if (totalRegistros === 0) {
          alert('No se encontraron registros válidos para procesar en el archivo.');
          setLoading(false);
          return;
        }

        // SELECCIÓN DINÁMICA DE ENDPOINTS SEGÚN ESTRUCTURA Y OPERACIÓN
        const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5243';
        let endpointDestino = '';

        if (tipoEstructura === 'simple') {
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
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify(bloque)
            });

            const data = await response.json();

            if (response.ok) {
              const resultadosBloque = data.detalles || data;
              if (Array.isArray(resultadosBloque)) {
                detallesAcumulados.push(...resultadosBloque);
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
          message: `Proceso de ${tipoOperacion.toUpperCase()} finalizado [Modalidad: ${tipoEstructura === 'simple' ? 'ARTÍCULOS SIMPLES' : 'COMBOS / BOM'}].`,
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
              />
              📦 Artículos / Producto Simple
            </label>
            <label style={styles.radioLabel}>
              <input 
                type="radio" 
                name="tipoEstructura" 
                checked={tipoEstructura === 'con_bom'} 
                onChange={() => setTipoEstructura('con_bom')} 
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
            Ejecutar {tipoOperacion.toUpperCase()} Masiva ({tipoEstructura === 'simple' ? 'Simples' : 'Combos'}) en Lotes
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
          <h4 style={{ margin: '0 0 10px 0' }}>✅ {resultado.message}</h4>
          <p>Total de registros procesados: <strong>{resultado.total}</strong></p>
          
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