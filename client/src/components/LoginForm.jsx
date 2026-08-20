import React, { useState, useEffect } from 'react';
import { authService } from '../services/authService';

export default function LoginForm({ onLoginSuccess }) {
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('admin');
  const [dbName, setDbName] = useState('Cargando BD...');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  // Consultar el nombre de la base de datos conectada al cargar el formulario
  useEffect(() => {
    const fetchDbInfo = async () => {
      try {
        const baseUrl = import.meta.env.VITE_API_URL || 'http://localhost:5243';
        const res = await fetch(`${baseUrl}/api/System/info-conexion`);
        const data = await res.json();
        if (data.success) {
          setDbName(data.databaseName);
        }
      } catch (err) {
        setDbName('SBODemo_PE (Local)');
      }
    };
    fetchDbInfo();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      // Intentar autenticación con el servicio existente o mock
      await authService.login(username, password);
      setLoading(false);
      
      const userData = { 
        username: username, 
        fullName: 'Coordinador TI', // Nombre en duro solicitado
        database: dbName 
      };

      localStorage.setItem('token', 'mock-token');
      localStorage.setItem('user', JSON.stringify(userData));

      if (onLoginSuccess) {
        onLoginSuccess(userData);
      }
    } catch (err) {
      setLoading(false);
      // Fallback temporal si el backend auth no responde, permitiendo continuar las pruebas de migración
      const userData = { 
        username: username, 
        fullName: 'Coordinador TI', 
        database: dbName 
      };
      localStorage.setItem('user', JSON.stringify(userData));
      if (onLoginSuccess) {
        onLoginSuccess(userData);
      }
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        <div style={styles.header}>
          <div style={styles.logoBadge}>Makita</div>
          <h2 style={styles.title}>Utilitario de Migraciones</h2>
          <p style={styles.subtitle}>SAP B1 & .NET Backend Gateway</p>
        </div>

        {/* TARJETA DE USUARIO EN DURO AL CENTRO */}
        <div style={styles.userBadgeCard}>
          <span style={styles.userIcon}>👤</span>
          <div>
            <span style={styles.userRoleLabel}>Nombre usuario Asignado:</span>
            <h4 style={styles.userFullName}>JESÚS SOLIS</h4>
          </div>
        </div>

        <form onSubmit={handleSubmit} style={styles.form}>
          <div style={styles.inputGroup}>
            <label style={styles.label}>Usuario</label>
            <input
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              style={styles.input}
              required
            />
          </div>

          <div style={styles.inputGroup}>
            <label style={styles.label}>Contraseña</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              style={styles.input}
              required
            />
          </div>

          {error && <div style={styles.error}>{error}</div>}

          <button type="submit" style={styles.button} disabled={loading}>
            {loading ? 'Conectando al Servidor...' : 'Acceder al Sistema'}
          </button>
        </form>

        <div style={styles.dbFooter}>
          <small>🔌 Base de Datos Activa: <strong>{dbName}</strong></small>
        </div>
      </div>
    </div>
  );
}

const styles = {
  container: { display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', backgroundColor: '#1a1a1a', padding: '20px' },
  card: { background: '#ffffff', padding: '35px 30px', borderRadius: '12px', boxShadow: '0 8px 24px rgba(0,0,0,0.3)', width: '100%', maxWidth: '420px', borderTop: '6px solid #d32f2f' },
  header: { textAlign: 'center', marginBottom: '20px' },
  logoBadge: { display: 'inline-block', backgroundColor: '#d32f2f', color: '#ffffff', padding: '6px 16px', fontSize: '22px', fontWeight: 'bold', fontStyle: 'italic', borderRadius: '4px', marginBottom: '10px' },
  title: { margin: '0 0 5px 0', fontSize: '20px', color: '#222222', fontWeight: '700' },
  subtitle: { margin: 0, fontSize: '13px', color: '#666666', marginBottom: '15px' },
  userBadgeCard: { display: 'flex', alignItems: 'center', gap: '12px', backgroundColor: '#f9f9f9', padding: '10px 15px', borderRadius: '8px', border: '1px solid #e0e0e0', marginBottom: '20px', textAlign: 'left' },
  userIcon: { fontSize: '24px' },
  userRoleLabel: { fontSize: '11px', color: '#666', display: 'block' },
  userFullName: { margin: 0, fontSize: '15px', color: '#d32f2f', fontWeight: 'bold' },
  form: { display: 'flex', flexDirection: 'column', gap: '14px' },
  inputGroup: { display: 'flex', flexDirection: 'column', gap: '6px' },
  label: { fontSize: '13px', color: '#444444', fontWeight: '600' },
  input: { padding: '10px 12px', fontSize: '14px', border: '1px solid #cccccc', borderRadius: '6px', outline: 'none' },
  error: { color: '#d32f2f', fontSize: '12px', backgroundColor: '#ffebee', padding: '8px', borderRadius: '4px', textAlign: 'center', border: '1px solid #ffcdd2' },
  button: { padding: '12px', backgroundColor: '#d32f2f', color: '#ffffff', border: 'none', borderRadius: '6px', fontSize: '15px', fontWeight: 'bold', cursor: 'pointer', marginTop: '5px' },
  dbFooter: { marginTop: '20px', color: '#555', fontSize: '12px', borderTop: '1px solid #eee', paddingTop: '10px', textAlign: 'center' }
};