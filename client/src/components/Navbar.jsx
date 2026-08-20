import React from 'react';

export default function Navbar({ user, databaseName, onLogout }) {
  return (
    <header style={styles.header}>
      <div style={styles.leftSection}>
        <span style={styles.brand}>Makita PE</span>
        <span style={styles.separator}>|</span>
        <span style={styles.moduleTitle}>Utilitarios & Migraciones</span>
      </div>

      <div style={styles.rightSection}>
        {/* INDICADOR DE BASE DE DATOS */}
        <div style={styles.dbBadge}>
          <span style={styles.dbDot}>🟢</span> 
          <span>BD: <strong>{databaseName || 'SAP_B1_DB'}</strong></span>
        </div>

        <div style={styles.userInfo}>
          <span style={styles.userAvatar}>👤</span>
          <span style={styles.userName}>{user?.fullName || user || 'Coordinador TI'}</span>
        </div>

        <button onClick={onLogout} style={styles.logoutBtn}>
          Cerrar Sesión
        </button>
      </div>
    </header>
  );
}

const styles = {
  header: { display: 'flex', justifyContent: 'space-between', alignItems: 'center', backgroundColor: '#1e1e1e', color: '#fff', padding: '12px 25px', borderBottom: '3px solid #d32f2f' },
  leftSection: { display: 'flex', alignItems: 'center', gap: '15px' },
  brand: { backgroundColor: '#d32f2f', color: '#fff', padding: '6px 12px', borderRadius: '4px', fontWeight: 'bold', fontSize: '15px' },
  separator: { color: '#555' },
  moduleTitle: { fontSize: '14px', color: '#ccc' },
  rightSection: { display: 'flex', alignItems: 'center', gap: '20px' },
  dbBadge: { display: 'flex', alignItems: 'center', gap: '6px', backgroundColor: '#2a2a2a', padding: '5px 10px', borderRadius: '4px', fontSize: '12px', color: '#ddd', border: '1px solid #444' },
  dbDot: { fontSize: '8px' },
  userInfo: { display: 'flex', alignItems: 'center', gap: '8px', fontSize: '14px', fontWeight: '500' },
  userAvatar: { fontSize: '16px' },
  userName: { color: '#fff' },
  logoutBtn: { backgroundColor: 'transparent', color: '#ff5252', border: '1px solid #ff5252', padding: '6px 14px', borderRadius: '4px', cursor: 'pointer', fontWeight: '600', transition: 'all 0.2s' }
};