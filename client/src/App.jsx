import React, { useState, useEffect } from 'react';
import LoginForm from './components/LoginForm';
import ArticulosModule from './components/ArticulosModule';

export default function App() {
  const [user, setUser] = useState(JSON.parse(localStorage.getItem('user')) || null);
  const [activeModule, setActiveModule] = useState('home');
  const [databaseName, setDatabaseName] = useState('Cargando BD...');

  // Consultar el nombre de la base de datos conectada en el backend al iniciar sesión
  useEffect(() => {
    if (user) {
      const fetchDbInfo = async () => {
        try {
          const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5243';
          const res = await fetch(`${baseUrl}/api/System/info-conexion`);
          const data = await res.json();
          if (data.success) {
            setDatabaseName(data.databaseName);
          }
        } catch (err) {
          setDatabaseName('SBODemo_PE (Local)');
        }
      };
      fetchDbInfo();
    }
  }, [user]);

  const handleLoginSuccess = (userData) => {
    // userData puede venir como objeto o string desde el LoginForm
    const userInfo = typeof userData === 'object' ? userData : { username: userData, fullName: 'Coordinador TI' };
    setUser(userInfo);
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setUser(null);
  };

  if (!user) {
    return <LoginForm onLoginSuccess={handleLoginSuccess} />;
  }

  return (
    <div style={styles.appContainer}>
      {/* Barra Superior / Header Responsive con BD y Usuario */}
      <header style={styles.header}>
        <div style={styles.brandContainer}>
          <span style={styles.logoMini}>Makita PE</span>
          <span style={styles.badgeUtil}>Utilitarios & Migraciones</span>
        </div>

        <div style={styles.headerRight}>
          {/* Indicador de Base de Datos */}
          <div style={styles.dbBadge}>
            <span style={styles.dbDot}>🟢</span> 
            <span>BD: <strong>{databaseName}</strong></span>
          </div>

          {/* Perfil de Usuario en duro */}
          <div style={styles.userContainer}>
            <span style={styles.userName}>👤 {user?.fullName || 'Coordinador TI'}</span>
            <button onClick={handleLogout} style={styles.logoutBtn}>Cerrar Sesión</button>
          </div>
        </div>
      </header>

      {/* Contenido Principal */}
      <main style={styles.mainContent}>
        {activeModule === 'home' && (
          <div>
            <div style={styles.welcomeBanner}>
              <h2>Panel de Control - Herramientas SAP B1</h2>
              <p>Selecciona el módulo utilitario que deseas ejecutar para procesar cargas masivas y sincronizaciones en tiempo real.</p>
            </div>

            {/* Grid Responsivo de Módulos */}
            <div style={styles.gridContainer}>
              <div style={styles.card} onClick={() => setActiveModule('articulos')}>
                <div style={styles.cardIcon}>📦</div>
                <h3>Carga Masiva de Artículos</h3>
                <p>Importación de productos simples y combos (Precios múltiples y Lista de Materiales).</p>
                <span style={styles.cardAction}>Entrar al módulo &rarr;</span>
              </div>

              <div style={styles.card} onClick={() => setActiveModule('socios')}>
                <div style={styles.cardIcon}>🤝</div>
                <h3>Gestión de Socios de Negocio</h3>
                <p>Administración de Clientes y Proveedores (RUC/DNI, codificación automática y anulación lógica).</p>
                <span style={styles.cardAction}>Entrar al módulo &rarr;</span>
              </div>

              <div style={styles.card} onClick={() => setActiveModule('stock')}>
                <div style={styles.cardIcon}>📊</div>
                <h3>Control de Stock y Movimientos</h3>
                <p>Monitoreo de ingresos, salidas de mercancía y cálculo de stock en tiempo real.</p>
                <span style={styles.cardAction}>Entrar al módulo &rarr;</span>
              </div>

              <div style={styles.card} onClick={() => setActiveModule('migraciones')}>
                <div style={styles.cardIcon}>⚡</div>
                <h3>Asistente de Migración masiva</h3>
                <p>Ejecución de scripts, validación de estructuras SQL Server y respaldos de sincronización.</p>
                <span style={styles.cardAction}>Entrar al módulo &rarr;</span>
              </div>
            </div>
          </div>
        )}

        {/* Vista modular limpia para Artículos */}
        {activeModule === 'articulos' && (
          <ArticulosModule onBack={() => setActiveModule('home')} />
        )}

        {/* Vistas para los demás submódulos */}
        {activeModule !== 'home' && activeModule !== 'articulos' && (
          <div style={styles.moduleView}>
            <button onClick={() => setActiveModule('home')} style={styles.backBtn}>
              &larr; Volver al Menú Principal
            </button>
            <div style={styles.moduleBox}>
              <h2>Módulo: {activeModule.toUpperCase()}</h2>
              <p>Aquí se cargará la interfaz específica para la gestión y procesamiento masivo de {activeModule}.</p>
              <div style={styles.placeholderBox}>
                <p>📥 [Área preparada para carga de archivos Excel / CSV y conexión con API .NET]</p>
              </div>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

const styles = {
  appContainer: { minHeight: '100vh', backgroundColor: '#f4f6f9', fontFamily: 'Segoe UI, Tahoma, Geneva, Verdana, sans-serif' },
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', backgroundColor: '#212121', color: '#fff', padding: '15px 30px', borderBottom: '4px solid #d32f2f', flexWrap: 'wrap', gap: '15px' },
  brandContainer: { display: 'flex', alignItems: 'center', gap: '15px' },
  logoMini: { backgroundColor: '#d32f2f', color: '#fff', padding: '4px 12px', fontWeight: 'bold', fontStyle: 'italic', fontSize: '18px', borderRadius: '4px' },
  badgeUtil: { fontSize: '14px', color: '#bbb', borderLeft: '1px solid #555', paddingLeft: '15px' },
  headerRight: { display: 'flex', alignItems: 'center', gap: '20px', flexWrap: 'wrap' },
  dbBadge: { display: 'flex', alignItems: 'center', gap: '6px', backgroundColor: '#2a2a2a', padding: '6px 12px', borderRadius: '6px', fontSize: '13px', color: '#ddd', border: '1px solid #444' },
  dbDot: { fontSize: '8px' },
  userContainer: { display: 'flex', alignItems: 'center', gap: '15px' },
  userName: { fontSize: '14px', color: '#ddd', fontWeight: '500' },
  logoutBtn: { backgroundColor: 'transparent', color: '#ff5252', border: '1px solid #ff5252', padding: '6px 12px', borderRadius: '4px', cursor: 'pointer', fontWeight: '600', transition: '0.2s' },
  mainContent: { maxWidth: '1200px', margin: '30px auto', padding: '0 20px' },
  welcomeBanner: { background: '#ffffff', padding: '25px 30px', borderRadius: '10px', boxShadow: '0 2px 8px rgba(0,0,0,0.05)', marginBottom: '30px', borderLeft: '5px solid #d32f2f' },
  gridContainer: { display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(260px, 1fr))', gap: '20px' },
  card: { background: '#ffffff', padding: '25px', borderRadius: '10px', boxShadow: '0 4px 12px rgba(0,0,0,0.06)', cursor: 'pointer', transition: 'transform 0.2s, box-shadow 0.2s', border: '1px solid #eaeaea', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' },
  cardIcon: { fontSize: '36px', marginBottom: '15px' },
  cardAction: { color: '#d32f2f', fontWeight: 'bold', fontSize: '13px', marginTop: '15px' },
  moduleView: { background: '#ffffff', padding: '30px', borderRadius: '10px', boxShadow: '0 4px 12px rgba(0,0,0,0.06)' },
  backBtn: { backgroundColor: '#e0e0e0', color: '#333', border: 'none', padding: '8px 16px', borderRadius: '4px', cursor: 'pointer', fontWeight: '600', marginBottom: '20px' },
  moduleBox: { marginTop: '15px' },
  placeholderBox: { border: '2px dashed #ccc', padding: '40px', textAlign: 'center', borderRadius: '8px', color: '#776', backgroundColor: '#fafafa', marginTop: '20px' }
};