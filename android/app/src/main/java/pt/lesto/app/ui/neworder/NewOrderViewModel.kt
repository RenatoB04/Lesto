package pt.lesto.app.ui.neworder

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import pt.lesto.app.data.model.CreateOrderDto
import pt.lesto.app.data.model.PointDto
import pt.lesto.app.data.network.ApiClient

// Esta classe define todos os dados que o ecrã precisa de ler
data class NewOrderUiState(
    val points: List<PointDto> = emptyList(),
    val originId: String? = null,
    val destinationId: String? = null,
    val weight: String = "",
    val type: String = "",
    val recipientData: String = "",
    val isLoading: Boolean = false,
    val error: String? = null,
    val success: Boolean = false
)

class NewOrderViewModel(private val context: Context) : ViewModel() {
    private val api = ApiClient.create(context)

    private val _uiState = MutableStateFlow(NewOrderUiState())
    val uiState: StateFlow<NewOrderUiState> = _uiState.asStateFlow()

    init {
        loadPoints()
    }

    private fun loadPoints() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true) }
            try {
                val response = api.getPoints()
                if (response.isSuccessful) {
                    _uiState.update { it.copy(points = response.body().orEmpty(), isLoading = false) }
                } else {
                    _uiState.update { it.copy(error = "Erro ${response.code()}: ${response.message()}", isLoading = false) }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(error = "Exceção: ${e.javaClass.simpleName} - ${e.message}", isLoading = false) }
            }
        }
    }

    fun onOriginSelected(id: String) {
        _uiState.update { it.copy(originId = id, error = null) }
    }

    fun onDestinationSelected(id: String) {
        _uiState.update { it.copy(destinationId = id, error = null) }
    }

    fun onWeightChange(weight: String) {
        _uiState.update { it.copy(weight = weight, error = null) }
    }

    fun onTypeChange(type: String) {
        _uiState.update { it.copy(type = type, error = null) }
    }

    fun onRecipientChange(data: String) {
        _uiState.update { it.copy(recipientData = data, error = null) }
    }

    fun submitOrder() {
        val currentState = _uiState.value
        val weightValue = currentState.weight.toDoubleOrNull()

        if (currentState.originId == null || currentState.destinationId == null ||
            weightValue == null || currentState.type.isBlank() || currentState.recipientData.isBlank()
        ) {
            _uiState.update { it.copy(error = "Preenche todos os campos corretamente.") }
            return
        }
        if (currentState.originId == currentState.destinationId) {
            _uiState.update { it.copy(error = "Origem e destino não podem ser o mesmo ponto.") }
            return
        }

        _uiState.update { it.copy(isLoading = true, error = null) }

        viewModelScope.launch {
            try {
                val response = api.createOrder(
                    CreateOrderDto(
                        originPointId = currentState.originId,
                        destinationPointId = currentState.destinationId,
                        weight = weightValue,
                        type = currentState.type,
                        recipientData = currentState.recipientData
                    )
                )
                if (response.isSuccessful) {
                    _uiState.update { it.copy(isLoading = false, success = true) }
                } else {
                    _uiState.update { it.copy(isLoading = false, error = "Erro ao criar encomenda (código ${response.code()}).") }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(isLoading = false, error = "Erro de ligação: ${e.message}") }
            }
        }
    }
}