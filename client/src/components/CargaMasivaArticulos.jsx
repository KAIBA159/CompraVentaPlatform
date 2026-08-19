// Archivo: src/components/CargaMasivaArticulos.jsx (Nuevo componente)

import React, { useState } from 'react';
import { articulosService } from '../services/articulosService'; // Servicio creado anteriormente

export default function CargaMasivaArticulos() {
  const [archivo, setArchivo] = useState(null);
  const [estadoCarga, setEstadoCarga] = useState({ cargando: false, mensaje: '', detalles: null });

  const handleFileChange = (event) => {
    // Aquí implementarías la lógica para leer el CSV/Excel y convertirlo a JSON DTO
    // Por ahora, solo guardamos el archivo seleccionado como referencia
    setArchivo(event.target.files[0]);
    setEstadoCarga({ cargando: false, mensaje: 'Archivo listo para procesar (simulación).', detalles: null });
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    
    if (!archivo) {
      setEstadoCarga({ cargando: false, mensaje: 'Por favor, selecciona un archivo.', detalles: null });
      return;
    }

    // SIMULACIÓN: Datos de prueba que coinciden con tu DTO en C#
    const articulosDePrueba = [
      { itemCode: 'MKP001', itemName: 'Taladro Percutor Makita 13mm', itemsGroupCode: 101 },
      { itemCode: 'MKP002', itemName: 'Esmeril Angular 4-1/2 Makita', itemsGroupCode: 101 },
    ];

    setEstadoCarga({ cargando: true, mensaje: 'Enviando datos al servidor .NET...', detalles: null });

    try {
      // Llamada al servicio que conecta con tu API
      const response = await articulosService.crearMasivos(articulosDePrueba);
      
      if (response.success) {
        setEstadoCarga({ 
            cargando: false, 
            mensaje: `✅ Carga exitosa: ${response.totalProcesados} artículos procesados correctamente.`, 
            detalles: response.detalles 
        });
      } else {
        throw new Error('El servidor retornó un estado de error.');
      }
    } catch (error) {
      console.error('Error:', error);
      setEstadoCarga({ cargando: false, mensaje: `❌ Error: ${error.message}`, detalles: null });
    }
  };

  return (
    <div style={styles.container}>
      <h2>📥 Carga Masiva de Artículos - Makita PE</h2>
      <p>Utilice este módulo para importar catálogos de productos desde archivos estructurados.</p>
      
      <form onSubmit={handleSubmit} style={styles.form}>
        <div style={styles.inputGroup}>
          <label>Seleccionar archivo (CSV/Excel):</label>
          <input type="file" onChange={handleFileChange} accept=".csv,.xlsx" style={styles.fileInput} />
        </div>

        <button type="submit" style={styles.button} disabled={estadoCarga.cargando}>
          {estadoCarga.cargando ? 'Procesando...' : 'Iniciar Carga Masiva'}
        </button>
      </form>

      {estadoCarga.mensaje && (
        <div style={{ ...styles.mensajeBox, backgroundColor: estadoCarga.mensaje.startsWith('✅') ? '#d4edda' : '#f8d7da' }}>
          <p>{estadoCarga.mensaje}</p>
          {estadoCarga.detalles && (
            <pre style={styles.detallesPre}>{JSON.stringify(estadoCarga.detalles, null, 2)}</pre>
          )}
        </div>
      )}
    </div>
  );
}

const styles = {
  container: { padding: '30px', background: '#fff', borderRadius: '8px', boxShadow: '0 2px 10px rgba(0,0,0,0.1)' },
  form: { display: 'flex', flexDirection: 'column', gap: '15px', marginTop: '20px' },
  inputGroup: { display: 'flex', flexDirection: 'column', gap: '5px' },
  fileInput: { padding: '10px', border: '1px solid #ccc', borderRadius: '4px' },
  button: { padding: '12px 20px', backgroundColor: '#d32f2f', color: '#fff', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' },
  mensajeBox: { marginTop: '20px', padding: '15px', borderRadius: '4px', border: '1px solid transparent' },
  detallesPre: { fontSize: '12px', background: '#eee', padding: '10px', overflow: 'auto' }
};