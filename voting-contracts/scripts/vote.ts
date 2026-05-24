import hre from "hardhat";

async function main() {

  const ethers =
    await hre.network.getOrCreate().then(n => n.ethers);

  const votingAddress = "0xa16E02E87b7454126E5E10d957A927A7F5B5d2be";

  const voting =
    await ethers.getContractAt("Voting", votingAddress);

  const tx = await voting.vote(0);

  await tx.wait();

  console.log("Vote cast successfully");
}

main().catch(console.error);