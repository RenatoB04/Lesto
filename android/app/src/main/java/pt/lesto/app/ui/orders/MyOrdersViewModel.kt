package pt.lesto.app.ui.orders

import android.content.Context
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import pt.lesto.app.data.model.OrderDto
import pt.lesto.app.data.network.ApiClient

data class MyOrdersUiState(
    val orders: List<OrderDto> = emptyList(),
    val isLoading: Boolean = false,
    val error: String? = null
)

class MyOrdersViewModel(context: Context) : ViewModel() {
    private val api = ApiClient.create(context)

    private val _uiState = MutableStateFlow(MyOrdersUiState())
    val uiState: StateFlow<MyOrdersUiState> = _uiState.asStateFlow()

    init {
        loadOrders()
    }

    fun loadOrders() {
        viewModelScope.launch {
            _uiState.update { it.copy(isLoading = true, error = null) }
            try {
                val response = api.getMyOrders()
                if (response.isSuccessful) {
                    _uiState.update { it.copy(orders = response.body().orEmpty(), isLoading = false) }
                } else {
                    _uiState.update { it.copy(error = "Erro ao carregar encomendas (código ${response.code()}).", isLoading = false) }
                }
            } catch (e: Exception) {
                _uiState.update { it.copy(error = "Erro de ligação: ${e.message}", isLoading = false) }
            }
        }
    }
}