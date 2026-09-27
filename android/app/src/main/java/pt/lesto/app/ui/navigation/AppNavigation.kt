package pt.lesto.app.ui.navigation

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import pt.lesto.app.ui.auth.AuthViewModel
import pt.lesto.app.ui.auth.LoginScreen
import pt.lesto.app.ui.auth.RegisterScreen

@Composable
fun AppNavigation(authViewModel: AuthViewModel) {
    val navController = rememberNavController()

    NavHost(navController = navController, startDestination = "login") {

        composable("login") {
            LoginScreen(
                viewModel = authViewModel,
                onLoginSuccess = { role ->
                    // Navega para o ecrã temporário e limpa o histórico de navegação
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
                    navController.popBackStack() // Volta ao login após registo
                },
                onNavigateBack = {
                    navController.popBackStack()
                }
            )
        }

        // Ecrã temporário para validar o sucesso do login (Tarefas 6.3 a 6.6 tratam do resto)
        composable("home/{role}") { backStackEntry ->
            val role = backStackEntry.arguments?.getString("role") ?: "Desconhecido"
            Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
                Text(text = "Bem-vindo! O teu perfil é: $role")
            }
        }
    }
}