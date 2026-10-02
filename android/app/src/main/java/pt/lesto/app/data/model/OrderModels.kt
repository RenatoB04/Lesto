package pt.lesto.app.data.model

import com.google.gson.annotations.SerializedName

data class PointDto(
    @SerializedName("id") val id: String,
    @SerializedName("name") val name: String
)

data class CreateOrderDto(
    @SerializedName("originPointId") val originPointId: String,
    @SerializedName("destinationPointId") val destinationPointId: String,
    @SerializedName("weight") val weight: Double,
    @SerializedName("type") val type: String,
    @SerializedName("recipientData") val recipientData: String
)

data class CreateOrderResponseDto(
    @SerializedName("message") val message: String,
    @SerializedName("orderId") val orderId: String
)

data class OrderDto(
    @SerializedName("id") val id: String,
    @SerializedName("status") val status: String,
    @SerializedName("weight") val weight: Double,
    @SerializedName("type") val type: String,
    @SerializedName("recipientData") val recipientData: String,
    @SerializedName("distance") val distance: Double,
    @SerializedName("duration") val duration: Double,
    @SerializedName("origin") val origin: PointDto,
    @SerializedName("destination") val destination: PointDto
)