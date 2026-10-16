import { z } from 'zod'
import { limits } from './limits'

// Field rules shared by the Register form and the admin's Create user form,
// so both always match the API's RegisterRequest / CreateUserRequest rules.

export const nameField = z
    .string()
    .trim()
    .min(1, 'Name is required')
    .max(limits.nameMax, `Name cannot be longer than ${limits.nameMax} characters`)

export const emailField = z
    .string()
    .trim()
    .min(1, 'Email is required')
    .email('Enter a valid email address')
    .max(limits.emailMax, `Email cannot be longer than ${limits.emailMax} characters`)

export const newPasswordField = z
    .string()
    .min(limits.passwordMin, `Password must be at least ${limits.passwordMin} characters`)
    .max(limits.passwordMax, `Password cannot be longer than ${limits.passwordMax} characters`)
