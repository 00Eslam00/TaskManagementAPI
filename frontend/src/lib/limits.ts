// Match the validation attributes and domain constants in the API,
// so the browser rejects exactly what the server would reject.
export const limits = {
  nameMax: 100, // User.NameMaxLength
  emailMax: 256, // User.EmailMaxLength
  passwordMin: 8, // RegisterRequest.Password
  passwordMax: 100,
  titleMax: 200, // TaskItem.TitleMaxLength
  descriptionMax: 2000, // TaskItem.DescriptionMaxLength
} as const
