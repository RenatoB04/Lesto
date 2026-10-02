package pt.lesto.app.data.network

import android.content.Context
import kotlinx.coroutines.flow.firstOrNull
import kotlinx.coroutines.runBlocking
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import pt.lesto.app.BuildConfig
import pt.lesto.app.data.SessionManager
import pt.lesto.app.data.model.AuthResponseDto
import pt.lesto.app.data.model.LoginDto
import pt.lesto.app.data.model.RegisterDto
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import retrofit2.http.Body
import retrofit2.http.POST
import pt.lesto.app.data.model.CreateOrderDto
import pt.lesto.app.data.model.CreateOrderResponseDto
import pt.lesto.app.data.model.OrderDto
import pt.lesto.app.data.model.PointDto
import retrofit2.http.GET

// 1. Definição dos Endpoints da API
interface ApiService {
    @POST("/api/auth/login")
    suspend fun login(@Body request: LoginDto): Response<AuthResponseDto>

    @POST("/api/auth/register")
    suspend fun register(@Body request: RegisterDto): Response<Any>

    @GET("/api/points")
    suspend fun getPoints(): Response<List<PointDto>>

    @POST("/api/orders")
    suspend fun createOrder(@Body request: CreateOrderDto): Response<CreateOrderResponseDto>

    @GET("/api/orders")
    suspend fun getMyOrders(): Response<List<OrderDto>>

}

// 2. Configuração do Cliente Retrofit
object ApiClient {
    // 10.0.2.2 é o IP para o emulador aceder ao localhost do computador
    private const val BASE_URL = BuildConfig.API_BASE_URL

    fun create(context: Context): ApiService {
        val sessionManager = SessionManager(context)

        // Interceptor para adicionar o Token JWT
        val authInterceptor = Interceptor { chain ->
            // Lê o token do DataStore. Usamos runBlocking porque o OkHttp não é assíncrono por omissão
            val token = runBlocking { sessionManager.tokenFlow.firstOrNull() }

            val requestBuilder = chain.request().newBuilder()
            if (!token.isNullOrEmpty()) {
                requestBuilder.addHeader("Authorization", "Bearer $token")
            }

            val response = chain.proceed(requestBuilder.build())

            // Se a API devolver 401 (Não Autorizado), podemos detetar aqui
            // A interface gráfica (ViewModels) vai tratar de redirecionar para o Login

            response
        }

        val okHttpClient = OkHttpClient.Builder()
            .addInterceptor(authInterceptor)
            .build()

        return Retrofit.Builder()
            .baseUrl(BASE_URL)
            .client(okHttpClient)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(ApiService::class.java)
    }
}