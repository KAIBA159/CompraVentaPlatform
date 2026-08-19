const API_URL = 'http://localhost:5243/api';

export const authService = {
  login: async (username, password) => {
    try {
      const response = await fetch(`${API_URL}/Auth/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password }),
      });

      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || 'Error en la autenticación');
      }

      return data;
    } catch (error) {
      throw error;
    }
  }
};