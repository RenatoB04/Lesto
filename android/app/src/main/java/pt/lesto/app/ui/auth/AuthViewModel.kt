package pt.lesto.app.ui.auth

import androidx.lifecycle.ViewModel
import androidx.lifecycle.ViewModelProvider
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.launch
import pt.lesto.app.data.SessionManager
import pt.lesto.app.data.model.LoginDto
import pt.lesto.app.data.model.RegisterDto
import pt.lesto.app.data.network.ApiService

class AuthViewModel(
    private val apiService: ApiService,
    private val sessionManager: SessionManager
) : ViewModel() {

    private val _uiState = MutableStateFlow<AuthState>(AuthState.Idle)
    val uiState: StateFlow<AuthState> = _uiState

    fun login(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = AuthState.Loading
            try {
                val response = apiService.login(LoginDto(email, password))
                if (response.isSuccessful && response.body() != null) {
                    val body = response.body()!!
                    sessionManager.saveSession(body.token, body.role, body.displayName)
                    _uiState.value = AuthState.Success(body.role)
                } else {
                    // Tratar erro 401 (Não autorizado)
                    _uiState.value = AuthState.Error("Credenciais inválidas")
                }
            } catch (e: Exception) {
                _uiState.value = AuthState.Error("Erro de ligação ao servidor")
            }
        }
    }

    fun register(name: String, email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = AuthState.Loading
            try {
                val response = apiService.register(RegisterDto(email, password, name))
                if (response.isSuccessful) {
                    _uiState.value = AuthState.Registered
                } else {
                    _uiState.value = AuthState.Error("Erro ao registar utilizador")
                }
            } catch (e: Exception) {
                _uiState.value = AuthState.Error("Erro de rede")
            }
        }
    }

    fun resetState() {
        _uiState.value = AuthState.Idle
    }
}

// Estados possíveis do ecrã
sealed class AuthState {
    object Idle : AuthState()
    object Loading : AuthState()
    object Registered : AuthState()
    data class Success(val role: String) : AuthState()
    data class Error(val message: String) : AuthState()
}

// Fábrica para injetar as dependências no ViewModel sem bibliotecas complexas
class AuthViewModelFactory(
    private val apiService: ApiService,
    private val sessionManager: SessionManager
) : ViewModelProvider.Factory {
    override fun <T : ViewModel> create(modelClass: Class<T>): T {
        return AuthViewModel(apiService, sessionManager) as T
    }
}