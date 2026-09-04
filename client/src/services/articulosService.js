// Archivo: src/services/articulosService.js
// Preparado para entorno Local (SQL Express) y Producción (Azure App Service)
const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5243/api';

export const articulosService = {
  
  // 1. POST: Crear nuevos registros (Simples y Combos)
  crearMasivos: async (articulos) => {
    const token = localStorage.getItem('token'); 

    try {
      const response = await fetch(`${API_URL}/Articles/CrearArticulosMasivos`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}` 
        },
        body: JSON.stringify(articulos),
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Error en la carga masiva de artículos');
      }

      return await response.json(); 
    } catch (error) {
      console.error('Error en servicio de creación de artículos:', error);
      throw error;
    }
  },

  // 2. PATCH: Actualizar Fabricante (OITM) y País de Origen (ITM10)
  actualizarFabricantePais: async (payload) => {
    const token = localStorage.getItem('token');

    try {
      const response = await fetch(`${API_URL}/articles/update-manufacturer`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || 'Error al actualizar registros existentes');
      }

      // Soporte para respuestas 204 No Content o respuestas con texto JSON
      const text = await response.text();
      return text ? JSON.parse(text) : { success: true, message: 'Actualización de datos maestros procesada' };
    } catch (error) {
      console.error('Error en la actualización de maestro:', error);
      throw error;
    }
  },

  // 3. PATCH: Actualizar Precio de una Lista Específica (Ej: Lista Brasil - 16)
  actualizarPrecioLista: async (payload) => {
    const token = localStorage.getItem('token');

    try {
      const response = await fetch(`${API_URL}/articles/update-price`, {
        method: 'PATCH',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || 'Error al actualizar precios de la lista');
      }

      const text = await response.text();
      return text ? JSON.parse(text) : { success: true, message: 'Precios actualizados exitosamente' };
    } catch (error) {
      console.error('Error en actualización de precios:', error);
      throw error;
    }
  }
};