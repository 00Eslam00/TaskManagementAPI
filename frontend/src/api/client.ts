import axios from 'axios'
import { config } from '../config'

// The single HTTP client for the whole app
export const apiClient = axios.create({
  baseURL: config.apiUrl,
  headers: { 'Content-Type': 'application/json' },
})
