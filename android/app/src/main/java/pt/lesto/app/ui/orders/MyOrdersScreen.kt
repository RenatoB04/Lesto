package pt.lesto.app.ui.orders

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.AssistChip
import androidx.compose.material3.AssistChipDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import pt.lesto.app.data.model.OrderDto
import androidx.compose.foundation.layout.Spacer

@Composable
fun MyOrdersScreen() {
    val context = LocalContext.current
    val viewModel = remember { MyOrdersViewModel(context) }
    val state by viewModel.uiState.collectAsState()

    Column(
        modifier = Modifier
            .fillMaxSize()
            .padding(16.dp)
    ) {
        Text("As minhas encomendas", style = MaterialTheme.typography.headlineSmall)
        Spacer(modifier = Modifier.height(12.dp))

        when {
            state.isLoading -> CircularProgressIndicator()
            state.error != null -> Text(state.error!!, color = MaterialTheme.colorScheme.error)
            state.orders.isEmpty() -> Text("Ainda não tens encomendas.")
            else -> LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
                items(state.orders) { order -> OrderCard(order) }
            }
        }
    }
}

@Composable
private fun OrderCard(order: OrderDto) {
    Card(modifier = Modifier.fillMaxWidth()) {
        Column(modifier = Modifier.padding(12.dp)) {
            Text("${order.origin.name} \u2192 ${order.destination.name}", style = MaterialTheme.typography.titleMedium)
            Text("Peso: ${order.weight} kg \u00b7 Tipo: ${order.type}")
            Spacer(modifier = Modifier.height(4.dp))
            StatusChip(order.status)
        }
    }
}

@Composable
private fun StatusChip(status: String) {
    AssistChip(onClick = {}, label = { Text(status) })
}