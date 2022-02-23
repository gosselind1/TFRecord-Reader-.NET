# TFRecord Reader (for .NET)
## Overview
This is meant to be a .NET library for reading Google's Tensorflow TFRecord Files *into* C# and other .NET langs

## Description
I built this library while working on my undergrad thesis at Southern Connecticut State University. It was built for the purpose of reading in many large .TFRecord files for machine learning research. The library was built with reference help from https://github.com/tensorflow/ecosystem/tree/master/hadoop/src/main/java/org/tensorflow/hadoop/util, and https://github.com/kevinskii/TFRecord.NET, which one might consider a companion library.

## Dependencies
This project depends on Google's protocol buffers and Crc32.NET (and should be easily compatible with any other CRC32 library as well based on some tinker I did). These dependencies should be easily downloadable if you're using anything that can read visual studio's files and can get NuGet(s).

## Example
	FileStream tfrecordFile = File.Open(filePath, FileMode.Open);
	long fileLength = tfrecordFile.Length;
	TFRecord_Reader.TFRecordReader reader = new TFRecord_Reader.TFRecordReader(tfrecordFile, true); // file reader
	
	while (fileLength > tfrecordFile.Position)
	{
		byte[] data;
		data = reader.Read(); // Becomes the next protocol buffer each iteration
		TFRecordReader.SequenceExample record = TFRecordReader.SequenceExample.Parser.ParseFrom(data);
	
		int label = (int)record.Context.Feature.GetValueOrDefault("label").Int64List.Value.ElementAt(0); // This is the label # of each sample.
	
		foreach (TFRecordReader.Feature points in record.FeatureLists.FeatureList.GetValueOrDefault("positions").Feature)
		{
			byte[] sample = points.BytesList.Value.ElementAt(0).ToArray();
	
			for (int index = 0; index < sample.Length; index += 8)
			{
				double xPositions = System.BitConverter.ToSingle(sample[(index + 0)..(index + 4)]); // + 0 for readability. Should be compiled out
				double yPositions = System.BitConverter.ToSingle(sample[(index + 4)..(index + 8)]);
			}
		}