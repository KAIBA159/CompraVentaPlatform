// Archivo: src/services/articulosService.js (Nuevo o Actualizado)

const API_URL = 'http://localhost:5243/api';

export const articulosService = {
  crearMasivos: async (articulos) => {
    // El token debe estar guardado en localStorage tras el login
    const token = localStorage.getItem('token'); 

    try {
      const response = await fetch(`${API_URL}/Articles/CrearArticulosMasivos`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}` // Seguridad JWT (implementar en .NET)
        },
        body: JSON.stringify(articulos), // Envía el array de objetos DTO
      });

      if (!response.ok) {
        // Manejo de errores HTTP (400, 500, etc.)
        const errorData = await response.json();
        throw new Error(errorData.message || 'Error en la carga masiva de artículos');
      }

      return await response.json(); // Retorna la respuesta { success: true, ... }
    } catch (error) {
      console.error('Error en servicio de artículos:', error);
      throw error;
    }
  }
};