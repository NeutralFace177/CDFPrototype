#version 400 core
out vec4 FragColor;
in vec2 vertexUV;

uniform sampler2D texture1;
uniform sampler2D texture2;
uniform sampler2D texture3;

void main() {
    vec2 vertexVU = vec2(vertexUV.y,vertexUV.x);
    FragColor = vec4(texture(texture1,vertexUV).x,texture(texture2,vertexUV).x,texture(texture3,vertexUV).x/2f, 1.0);
}






