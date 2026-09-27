package pt.lesto.app.ui.theme

import android.app.Activity
import android.os.Build
import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.SideEffect
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.toArgb
import androidx.compose.ui.platform.LocalView
import androidx.core.view.WindowCompat

// Mapeamento das cores da Lesto para o Material 3
private val LestoColorScheme = lightColorScheme(
    primary = Petroleo,
    onPrimary = Color.White,
    secondary = LaranjaSinal,
    onSecondary = TextoNoite,
    background = FundoNevoa,
    onBackground = TextoNoite,
    surface = BrancoCartao,
    onSurface = TextoNoite,
    outline = Borda
)

@Composable
fun LestoTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    // Para este projeto, vamos forçar o tema claro para garantir que a paleta
    // corporativa é sempre respeitada, ignorando o dark mode do sistema.
    val colorScheme = LestoColorScheme
    val view = LocalView.current

    if (!view.isInEditMode) {
        SideEffect {
            val window = (view.context as Activity).window
            window.statusBarColor = colorScheme.primary.toArgb()
            WindowCompat.getInsetsController(window, view).isAppearanceLightStatusBars = false
        }
    }

    MaterialTheme(
        colorScheme = colorScheme,
        typography = Typography, // Usa a tipografia padrão do Compose
        content = content
    )
}