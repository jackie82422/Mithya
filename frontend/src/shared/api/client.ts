import axios from 'axios';

const apiClient = axios.create({
  baseURL: '/admin/api',
  headers: { 'Content-Type': 'application/json' },
});

export default apiClient;
