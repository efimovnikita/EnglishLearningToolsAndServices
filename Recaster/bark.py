import numpy as np
import nltk
import json
import argparse
import time

from bark import SAMPLE_RATE, generate_audio, preload_models
from scipy.io.wavfile import write as write_wav

# Record the start time
start_time = time.time()

nltk.download('punkt')  # needed for sentence tokenization


def chunk_string(s, max_chunk_size):
    sentences = nltk.sent_tokenize(s)
    chunks = []
    current_chunk = ""
    for sentence in sentences:
        # if adding the next sentence doesn't exceed the max_chunk_size, add the sentence to the chunk
        if len(current_chunk) + len(sentence) <= max_chunk_size:
            current_chunk += " " + sentence
        else:
            # if it does exceed the max_chunk_size, add the current_chunk to the chunks list, and start a new chunk
            chunks.append(current_chunk)
            current_chunk = sentence
    # add the last chunk to the chunks list
    chunks.append(current_chunk)
    return chunks


def parse_json(file_path):
    with open(file_path, 'r') as json_file:
        data = json.load(json_file)
    return data


# download and load all models
preload_models()

# parse command line argument
parser = argparse.ArgumentParser()
parser.add_argument("--json_file", type=str, help="Path to the JSON file")
args = parser.parse_args()

# parse json data
data = parse_json(args.json_file)

# list of speakers
speakers = ["v2/en_speaker_6", "v2/en_speaker_9"]

audio_arrays = []
speaker_index = 0  # start with the first speaker

# generate audio for each transcript
for transcript in data["transcript"]:
    speech = transcript["speech"]
    chunks = chunk_string(speech, 210)
    speaker_index = (speaker_index + 1) % len(speakers)  # cycle through the speakers
# generate audio for each chunk and concatenate
    for chunk in chunks:
        audio_arrays.append(generate_audio(chunk, history_prompt=speakers[speaker_index]))


# concatenate all the audio arrays
audio_array = np.concatenate(audio_arrays)

# save audio to disk
write_wav("bark_generation.wav", SAMPLE_RATE, audio_array)

# Record the end time
end_time = time.time()

# Calculate the elapsed time
elapsed_time = end_time - start_time

# Print the elapsed time
print(f"Elapsed time: {elapsed_time} seconds")
