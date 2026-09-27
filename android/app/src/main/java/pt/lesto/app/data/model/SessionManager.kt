package pt.lesto.app.data

import android.content.Context
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import androidx.datastore.preferences.preferencesDataStore
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

// Cria a instância do DataStore de forma global (Singleton)
val Context.dataStore: DataStore<Preferences> by preferencesDataStore(name = "lesto_session")

class SessionManager(private val context: Context) {

    companion object {
        val JWT_TOKEN_KEY = stringPreferencesKey("jwt_token")
        val USER_ROLE_KEY = stringPreferencesKey("user_role")
        val USER_NAME_KEY = stringPreferencesKey("user_name")
    }

    // Grava os dados do login
    suspend fun saveSession(token: String, role: String, name: String) {
        context.dataStore.edit { preferences ->
            preferences[JWT_TOKEN_KEY] = token
            preferences[USER_ROLE_KEY] = role
            preferences[USER_NAME_KEY] = name
        }
    }

    // Apaga os dados (Logout ou erro 401)
    suspend fun clearSession() {
        context.dataStore.edit { preferences ->
            preferences.clear()
        }
    }

    // Lê o Token de forma reativa (Flow)
    val tokenFlow: Flow<String?> = context.dataStore.data.map { preferences ->
        preferences[JWT_TOKEN_KEY]
    }

    // Lê a Role
    val roleFlow: Flow<String?> = context.dataStore.data.map { preferences ->
        preferences[USER_ROLE_KEY]
    }
}