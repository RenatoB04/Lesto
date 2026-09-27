package pt.lesto.app.data.model

import com.google.gson.annotations.SerializedName

data class LoginDto(
    val email: String,
    val password: String
)

data class RegisterDto(
    val email: String,
    val password: String,
    val displayName: String
)

data class AuthResponseDto(
    val token: String,
    val email: String,
    val displayName: String,
    val role: String
)