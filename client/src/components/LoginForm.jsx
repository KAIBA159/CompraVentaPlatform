import React, { useState } from 'react';
import { authService } from '../services/authService';

export default function LoginForm({ onLoginSuccess }) {
  const [username, setUsername] = useState('admin');
  const [password, setPassword] = useState('admin');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const data = await authService.login(username, password);
      setLoading(false);
      localStorage.setItem('token', data.token || 'mock-token');
      localStorage.setItem('user', data.user || username);
      if (onLoginSuccess) {
        onLoginSuccess(data.user || username);
      }
    } catch (err) {
      setLoading(false);
      setError('No se pudo conectar con el Backend (C#). Verifica que la API esté corriendo en el puerto 5243.');
    }
  };

  return (
    <div style={styles.container}>
      <div style={styles.card}>
        {/* Logo / Header Makita */}
        <div style={styles.header}>
          <div style={styles.logoBadge}>Makita</div> {/* Estilo tipográfico industrial */}
          <h2 style={styles.title}>Utilitario de Migraciones</h2>
          <p style={styles.subtitle}>SAP B1 & .NET Backend Gateway</p>
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
      </div>
    </div>
  );
}

const styles = {
  container: { display: 'flex', justifyContent: 'center', alignItems: 'center', minHeight: '100vh', backgroundColor: '#1a1a1a', padding: '20px' },
  card: { background: '#ffffff', padding: '40px 30px', borderRadius: '12px', boxShadow: '0 8px 24px rgba(0,0,0,0.3)', width: '100%', maxWidth: '420px', borderTop: '6px solid #d32f2f' },
  header: { textAlign: 'center', marginBottom: '25px' },
  logoBadge: { display: 'inline-block', backgroundColor: '#d32f2f', color: '#ffffff', padding: '6px 16px', fontSize: '24px', fontWeight: 'bold', fontStyle: 'italic', borderRadius: '4px', letterSpacing: '1px', marginBottom: '12px', boxShadow: '0 2px 5px rgba(211,47,47,0.4)' },
  title: { margin: '0 0 5px 0', fontSize: '20px', color: '#222222', fontWeight: '700' },
  subtitle: { margin: 0, fontSize: '13px', color: '#666666' },
  form: { display: 'flex', flexDirection: 'column', gap: '16px' },
  inputGroup: { display: 'flex', flexDirection: 'column', gap: '6px' },
  label: { fontSize: '13px', color: '#444444', fontWeight: '600' },
  input: { padding: '12px', fontSize: '15px', border: '1px solid #cccccc', borderRadius: '6px', outline: 'none', transition: 'border 0.2s' },
  error: { color: '#d32f2f', fontSize: '12px', backgroundColor: '#ffebee', padding: '10px', borderRadius: '4px', textAlign: 'center', border: '1px solid #ffcdd2' },
  button: { padding: '12px', backgroundColor: '#d32f2f', color: '#ffffff', border: 'none', borderRadius: '6px', fontSize: '15px', fontWeight: 'bold', cursor: 'pointer', transition: 'background 0.2s', marginTop: '10px' },
};