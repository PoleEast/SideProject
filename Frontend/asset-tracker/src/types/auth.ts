export interface LoginRequest {
  account: string
  password: string
}

export interface RegisterRequest {
  account: string
  password: string
  name: string
}

export interface AuthResponse {
  token: string
}
