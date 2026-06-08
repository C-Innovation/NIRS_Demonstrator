using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using static System.Collections.Specialized.BitVector32;

namespace NIRS_Demonstrator.Helpers.AI
{
    /// <summary>
    /// 
    /// </summary>
    public class TestONNX : IDisposable
    {
        private InferenceSession? session;
        private string inputName;

        public TestONNX(string modelPath = "model_exports/model.onnx")
        {
            // Загружаем модель
            session = new InferenceSession(modelPath);

            // Получаем имя входного тензора
            inputName = session.InputMetadata.Keys.First();
            Console.WriteLine($"Модель загружена. Вход: {inputName}");
            Console.WriteLine($"Форма входа: [{string.Join(", ", session.InputMetadata[inputName].Dimensions)}]");
        }

        /// <summary>
        /// Выполняет инференс для одного набора входов
        /// </summary>
        /// <param name="inputs">Массив из 8 значений (float32)</param>
        /// <returns>Результат предсказания (0..1)</returns>
        public float Predict(float[] inputs)
        {
            if (inputs.Length != 8)
                throw new ArgumentException($"Ожидается 8 входов, получено {inputs.Length}");

            // Создаем тензор с правильной размерностью [1, 8]
            var tensor = new DenseTensor<float>(inputs, new[] { 1, 8 });

            // Создаем NamedOnnxValue для входа
            var inputValues = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(inputName, tensor)
        };

            // Выполняем инференс
            using (var results = session.Run(inputValues))
            {
                // Получаем результат (первый выход)
                var output = results.First();

                // Извлекаем значение
                float prediction;
                if (output.ValueType == OnnxValueType.ONNX_TYPE_TENSOR)
                {
                    var outputTensor = output.AsTensor<float>();
                    prediction = outputTensor.GetValue(0);
                }
                else
                {
                    throw new InvalidOperationException($"Неподдерживаемый тип выхода: {output.ValueType}");
                }

                return prediction;
            }
        }

        /// <summary>
        /// Выполняет инференс для нескольких наборов входов (batch)
        /// </summary>
        public float[] PredictBatch(float[][] inputsBatch)
        {
            var results = new float[inputsBatch.Length];

            for (int i = 0; i < inputsBatch.Length; i++)
            {
                results[i] = Predict(inputsBatch[i]);
            }

            return results;
        }

        public void Dispose()
        {
            session?.Dispose();
        }
    }
}
