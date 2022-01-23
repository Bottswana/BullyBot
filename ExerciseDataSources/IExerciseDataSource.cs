using BullyBot.Models;
using System.Threading.Tasks;

namespace BullyBot.ExerciseDataSources;
public interface IExerciseDataSource
{
    public Task<ExerciseDataModel> DownloadData();
}