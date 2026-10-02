package pt.lesto.app.ui.navigation

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.material3.Button
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import pt.lesto.app.ui.auth.AuthViewModel
import pt.lesto.app.ui.auth.LoginScreen
import pt.lesto.app.ui.auth.RegisterScreen
import pt.lesto.app.ui.neworder.NewOrderScreen
import pt.lesto.app.ui.orders.MyOrdersScreen

@Composable
fun AppNavigation(authViewModel: AuthViewModel) {
    val navController = rememberNavController()

    NavHost(navController = navController, startDestination = "login") {

        composable("login") {
            LoginScreen(
                viewModel = authViewModel,
                onLoginSuccess = { role ->
                    navController.navigate("home/$role") {
                        popUpTo("login") { inclusive = true }
                    }
                },
                onNavigateToRegister = {
                    navController.navigate("register")
                }
            )
        }

        composable("register") {
            RegisterScreen(
                viewModel = authViewModel,
                onRegisterSuccess = {
                    navController.popBackStack()
                },
                onNavigateBack = {
                    navController.popBackStack()
                }
            )
        }

        composable("home/{role}") { backStackEntry ->
            val role = backStackEntry.arguments?.getString("role") ?: "Desconhecido"
            Column(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(16.dp),
                verticalArrangement = Arrangement.spacedBy(16.dp)
            ) {
                Text("Bem-vindo! O teu perfil é: $role")

                Button(onClick = { navController.navigate("newOrder") }) {
                    Text("Nova encomenda")
                }

                Button(onClick = { navController.navigate("myOrders") }) {
                    Text("As minhas encomendas")
                }
            }
        }

        composable("newOrder") {
            NewOrderScreen(
                onOrderCreated = {
                    navController.popBackStack()
                }
            )
        }

        composable("myOrders") {
            MyOrdersScreen()
        }
    }
}