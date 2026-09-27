package pt.lesto.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.ui.Modifier
import androidx.lifecycle.ViewModelProvider
import pt.lesto.app.data.SessionManager
import pt.lesto.app.data.network.ApiClient
import pt.lesto.app.ui.auth.AuthViewModel
import pt.lesto.app.ui.auth.AuthViewModelFactory
import pt.lesto.app.ui.navigation.AppNavigation
import pt.lesto.app.ui.theme.LestoTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // Instanciar dependências base
        val sessionManager = SessionManager(applicationContext)
        val apiService = ApiClient.create(applicationContext)

        val factory = AuthViewModelFactory(apiService, sessionManager)
        val authViewModel = ViewModelProvider(this, factory)[AuthViewModel::class.java]

        setContent {
            LestoTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    AppNavigation(authViewModel = authViewModel)
                }
            }
        }
    }
}