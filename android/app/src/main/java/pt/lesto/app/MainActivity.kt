package pt.lesto.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import pt.lesto.app.ui.theme.LestoTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            LestoTheme {
                // A Surface adota automaticamente o FundoNevoa definido no Theme.kt
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = MaterialTheme.colorScheme.background
                ) {
                    TestScreen()
                }
            }
        }
    }
}

@Composable
fun TestScreen() {
    Column(
        modifier = Modifier.fillMaxSize(),
        verticalArrangement = Arrangement.Center,
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        // Exibe o logótipo que adicionaste à pasta drawable
        Image(
            painter = painterResource(id = R.drawable.logo_lesto),
            contentDescription = "Logótipo Lesto",
            modifier = Modifier.size(150.dp)
        )

        Spacer(modifier = Modifier.height(32.dp))

        Text(
            text = "Bem-vindo à Lesto",
            style = MaterialTheme.typography.headlineMedium,
            fontWeight = FontWeight.Bold,
            color = MaterialTheme.colorScheme.primary // Fica na cor Petróleo
        )

        Spacer(modifier = Modifier.height(16.dp))

        Button(
            onClick = { /* Sem ação para já */ },
            colors = ButtonDefaults.buttonColors(
                containerColor = MaterialTheme.colorScheme.secondary // Fica Laranja Sinal
            )
        ) {
            Text(
                text = "Testar Tema",
                color = MaterialTheme.colorScheme.onSecondary // Fica Texto Noite
            )
        }
    }
}